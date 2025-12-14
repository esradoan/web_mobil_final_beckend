using QRCoder;
using SmartCampus.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IQrCodeService
    {
        /// <summary>
        /// QR kod görselini Base64 string olarak döndürür
        /// </summary>
        Task<QrCodeImageDto> GenerateQrCodeImageAsync(int sessionId);
        
        /// <summary>
        /// QR kodu 5 saniyede bir yeniler
        /// </summary>
        Task<string> RefreshQrCodeAsync(int sessionId, int instructorId);
        
        /// <summary>
        /// QR kod ile check-in (GPS + QR doğrulama)
        /// </summary>
        Task<QrCheckInResponseDto> CheckInWithQrAsync(int sessionId, int studentId, QrCheckInRequestDto dto, string? clientIp);
        
        /// <summary>
        /// QR kodu doğrula
        /// </summary>
        Task<bool> ValidateQrCodeAsync(int sessionId, string qrCode);
    }

    public class QrCodeImageDto
    {
        public string QrCode { get; set; } = string.Empty;
        public string ImageBase64 { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int SecondsRemaining { get; set; }
    }

    public class QrCheckInRequestDto
    {
        public string QrCode { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal Accuracy { get; set; }
        public bool? IsMockLocation { get; set; }
    }

    public class QrCheckInResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? Distance { get; set; }
        public bool IsFlagged { get; set; }
        public string? FlagReason { get; set; }
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly CampusDbContext _context;
        private const int QR_REFRESH_SECONDS = 5;
        private const double EarthRadiusMeters = 6371000;

        public QrCodeService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<QrCodeImageDto> GenerateQrCodeImageAsync(int sessionId)
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session == null)
                throw new Exception("Session not found");

            // Generate QR code image
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(session.QrCode, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);
            var base64Image = Convert.ToBase64String(qrCodeBytes);

            var secondsRemaining = (int)(session.QrCodeExpiry - DateTime.UtcNow).TotalSeconds;

            return new QrCodeImageDto
            {
                QrCode = session.QrCode,
                ImageBase64 = $"data:image/png;base64,{base64Image}",
                ExpiresAt = session.QrCodeExpiry,
                SecondsRemaining = Math.Max(0, secondsRemaining)
            };
        }

        public async Task<string> RefreshQrCodeAsync(int sessionId, int instructorId)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.InstructorId == instructorId);

            if (session == null)
                throw new UnauthorizedAccessException("Not authorized");

            if (session.Status != "active")
                throw new InvalidOperationException("Session is not active");

            // Generate new QR code (16 character unique code)
            session.QrCode = GenerateUniqueCode();
            session.QrCodeExpiry = DateTime.UtcNow.AddSeconds(QR_REFRESH_SECONDS);
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return session.QrCode;
        }

        public async Task<QrCheckInResponseDto> CheckInWithQrAsync(
            int sessionId, 
            int studentId, 
            QrCheckInRequestDto dto, 
            string? clientIp)
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session == null || session.Status != "active")
            {
                return new QrCheckInResponseDto
                {
                    Success = false,
                    Message = "Session not found or closed"
                };
            }

            // 1. Validate QR code
            if (session.QrCode != dto.QrCode)
            {
                return new QrCheckInResponseDto
                {
                    Success = false,
                    Message = "Invalid QR code - code may have expired",
                    IsFlagged = true,
                    FlagReason = "Invalid QR code"
                };
            }

            // 2. Check QR expiry
            if (DateTime.UtcNow > session.QrCodeExpiry)
            {
                return new QrCheckInResponseDto
                {
                    Success = false,
                    Message = "QR code expired - please scan the latest code",
                    IsFlagged = true,
                    FlagReason = "Expired QR code"
                };
            }

            // 3. Check if already checked in
            var existing = await _context.AttendanceRecords
                .AnyAsync(r => r.SessionId == sessionId && r.StudentId == studentId);

            if (existing)
            {
                return new QrCheckInResponseDto
                {
                    Success = false,
                    Message = "Already checked in"
                };
            }

            // 4. Calculate distance (GPS validation)
            double distance = CalculateHaversineDistance(
                (double)dto.Latitude, (double)dto.Longitude,
                (double)session.Latitude, (double)session.Longitude);

            // 5. Check spoofing
            var flags = new List<string>();
            
            if (distance > (double)session.GeofenceRadius + 10.0) // More lenient for QR (10m buffer)
            {
                flags.Add($"Distance: {distance:F1}m");
            }
            
            if (dto.Accuracy > 100) // More lenient for QR
            {
                flags.Add($"Low accuracy: {dto.Accuracy:F1}m");
            }
            
            if (dto.IsMockLocation == true)
            {
                flags.Add("Mock location");
            }

            var isFlagged = flags.Any();
            var flagReason = isFlagged ? string.Join("; ", flags) : null;

            // 6. Create attendance record
            var record = new Entities.AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = studentId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DistanceFromCenter = (decimal)distance,
                IsFlagged = isFlagged,
                FlagReason = flagReason,
                CheckInTime = DateTime.UtcNow
            };

            _context.AttendanceRecords.Add(record);
            await _context.SaveChangesAsync();

            return new QrCheckInResponseDto
            {
                Success = true,
                Message = isFlagged ? "Check-in recorded with warnings" : "Check-in successful",
                Distance = (decimal)distance,
                IsFlagged = isFlagged,
                FlagReason = flagReason
            };
        }

        public async Task<bool> ValidateQrCodeAsync(int sessionId, string qrCode)
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session == null) return false;

            // Check QR code match and expiry
            return session.QrCode == qrCode && 
                   session.Status == "active" &&
                   DateTime.UtcNow <= session.QrCodeExpiry;
        }

        private string GenerateUniqueCode()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper() + 
                   DateTime.UtcNow.Ticks.ToString()[^4..];
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMeters * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
