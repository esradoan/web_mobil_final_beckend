using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceSessionDto> CreateSessionAsync(int instructorId, CreateAttendanceSessionDto dto);
        Task<AttendanceSessionDto?> GetSessionByIdAsync(int id);
        Task<bool> CloseSessionAsync(int sessionId, int instructorId);
        Task<List<AttendanceSessionDto>> GetMySessionsAsync(int instructorId);
        Task<CheckInResponseDto> CheckInAsync(int sessionId, int studentId, CheckInRequestDto dto, string? clientIp);
        Task<List<MyAttendanceDto>> GetMyAttendanceAsync(int studentId);
        Task<AttendanceReportDto> GetAttendanceReportAsync(int sectionId);
        Task<ExcuseRequestDto> CreateExcuseRequestAsync(int studentId, CreateExcuseRequestDto dto, string? documentUrl);
        Task<List<ExcuseRequestDto>> GetExcuseRequestsAsync(int instructorId);
        Task<bool> ApproveExcuseAsync(int requestId, int reviewerId, string? notes);
        Task<bool> RejectExcuseAsync(int requestId, int reviewerId, string? notes);
        string GenerateQrCode();
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly CampusDbContext _context;
        private readonly INotificationService? _notificationService;
        private const double EarthRadiusMeters = 6371000;

        public AttendanceService(CampusDbContext context, INotificationService? notificationService = null)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<AttendanceSessionDto> CreateSessionAsync(int instructorId, CreateAttendanceSessionDto dto)
        {
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == dto.SectionId && s.InstructorId == instructorId);

            if (section == null)
                throw new UnauthorizedAccessException("Not authorized for this section");

            // Get classroom GPS coordinates
            decimal latitude = section.Classroom?.Latitude ?? 0;
            decimal longitude = section.Classroom?.Longitude ?? 0;

            var session = new AttendanceSession
            {
                SectionId = dto.SectionId,
                InstructorId = instructorId,
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Latitude = latitude,
                Longitude = longitude,
                GeofenceRadius = dto.GeofenceRadius ?? 15.0m,
                QrCode = GenerateQrCode(),
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(30),
                Status = "active"
            };

            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Send notification to enrolled students
            if (_notificationService != null)
            {
                _ = _notificationService.SendSessionStartNotificationAsync(dto.SectionId, session.Id);
            }

            return await MapToSessionDtoAsync(session);
        }

        public async Task<AttendanceSessionDto?> GetSessionByIdAsync(int id)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Records)
                .FirstOrDefaultAsync(s => s.Id == id);

            return session == null ? null : await MapToSessionDtoAsync(session);
        }

        public async Task<bool> CloseSessionAsync(int sessionId, int instructorId)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.InstructorId == instructorId);

            if (session == null) return false;

            session.Status = "closed";
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AttendanceSessionDto>> GetMySessionsAsync(int instructorId)
        {
            var sessions = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Records)
                .Where(s => s.InstructorId == instructorId)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            var result = new List<AttendanceSessionDto>();
            foreach (var s in sessions)
            {
                result.Add(await MapToSessionDtoAsync(s));
            }
            return result;
        }

        public async Task<CheckInResponseDto> CheckInAsync(int sessionId, int studentId, CheckInRequestDto dto, string? clientIp)
        {
            var session = await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == "active");

            if (session == null)
                throw new Exception("Session not found or closed");

            // Check if already checked in
            var existing = await _context.AttendanceRecords
                .AnyAsync(r => r.SessionId == sessionId && r.StudentId == studentId);

            if (existing)
                throw new InvalidOperationException("Already checked in");

            // Calculate distance using Haversine formula
            double distance = CalculateHaversineDistance(
                (double)dto.Latitude, (double)dto.Longitude,
                (double)session.Latitude, (double)session.Longitude);

            // Advanced spoofing detection (async with velocity check)
            var (isFlagged, flagReason) = await DetectSpoofingAsync(dto, session, distance, clientIp, studentId);

            var record = new AttendanceRecord
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

            // Check if distance exceeded (but still record the attempt)
            if (distance > (double)session.GeofenceRadius + 5.0)
            {
                return new CheckInResponseDto
                {
                    Message = $"You are too far from the classroom. Distance: {distance:F1}m",
                    Distance = (decimal)distance,
                    IsFlagged = true,
                    FlagReason = "Distance exceeded"
                };
            }

            return new CheckInResponseDto
            {
                Message = "Check-in successful",
                Distance = (decimal)distance,
                IsFlagged = isFlagged,
                FlagReason = flagReason
            };
        }

        /// <summary>
        /// Haversine formula - iki GPS noktası arasındaki mesafeyi metre cinsinden hesaplar
        /// </summary>
        public double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
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

        /// <summary>
        /// Gelişmiş GPS Spoofing Tespit Sistemi
        /// - Distance check: Geofence dışında mı?
        /// - Accuracy check: GPS doğruluğu düşük mü?
        /// - Mock location: Sahte konum uygulaması kullanılıyor mu?
        /// - Velocity check: İmkansız hız tespit edildi mi?
        /// - IP validation: Kampüs ağında mı?
        /// </summary>
        private async Task<(bool IsFlagged, string? Reason)> DetectSpoofingAsync(
            CheckInRequestDto dto, 
            AttendanceSession session, 
            double distance, 
            string? clientIp,
            int studentId)
        {
            var flags = new List<string>();

            // 1. Distance check
            if (distance > (double)session.GeofenceRadius + 5.0)
            {
                flags.Add($"Distance exceeded: {distance:F1}m");
            }

            // 2. Accuracy check (if accuracy is too low, might be spoofed)
            if (dto.Accuracy > 50)
            {
                flags.Add($"Low accuracy: {dto.Accuracy:F1}m");
            }

            // 3. Mock location detection (from device API)
            if (dto.IsMockLocation == true)
            {
                flags.Add("Mock location detected");
            }

            // 4. Velocity check (impossible travel detection)
            var velocityFlag = await CheckImpossibleVelocityAsync(studentId, dto);
            if (!string.IsNullOrEmpty(velocityFlag))
            {
                flags.Add(velocityFlag);
            }

            // 5. IP validation (campus network check)
            var ipFlag = ValidateCampusNetwork(clientIp);
            if (!string.IsNullOrEmpty(ipFlag))
            {
                flags.Add(ipFlag);
            }

            // 6. Speed validation (device reported speed)
            if (dto.Speed.HasValue && dto.Speed > 5) // Walking speed is ~1.4 m/s, running ~3 m/s
            {
                flags.Add($"Suspicious speed: {dto.Speed:F1} m/s");
            }

            if (flags.Any())
            {
                return (true, string.Join("; ", flags));
            }

            return (false, null);
        }

        /// <summary>
        /// İmkansız Seyahat Kontrolü (Velocity Check)
        /// Son check-in ile mevcut konum arasındaki mesafeyi süreye bölerek hız hesaplar.
        /// İnsan için imkansız hızlar tespit edilirse flag atar.
        /// </summary>
        private async Task<string?> CheckImpossibleVelocityAsync(int studentId, CheckInRequestDto dto)
        {
            // Get the last attendance record for this student
            var lastRecord = await _context.AttendanceRecords
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CheckInTime)
                .FirstOrDefaultAsync();

            if (lastRecord == null) return null;

            // Calculate time difference
            var timeDiff = DateTime.UtcNow - lastRecord.CheckInTime;
            
            // If less than 1 minute, might be duplicate (ignore)
            if (timeDiff.TotalMinutes < 1) return null;

            // Calculate distance from last location
            var distanceFromLast = CalculateHaversineDistance(
                (double)lastRecord.Latitude, (double)lastRecord.Longitude,
                (double)dto.Latitude, (double)dto.Longitude);

            // Calculate velocity (m/s)
            var velocity = distanceFromLast / timeDiff.TotalSeconds;

            // Max reasonable walking/running speed: 10 m/s (~36 km/h)
            // If velocity > 50 m/s (180 km/h), definitely spoofed
            if (velocity > 50)
            {
                return $"Impossible velocity: {velocity:F1} m/s ({velocity * 3.6:F0} km/h)";
            }

            // Suspicious if > 20 m/s (72 km/h) within campus
            if (velocity > 20 && timeDiff.TotalMinutes < 5)
            {
                return $"Suspicious velocity: {velocity:F1} m/s in {timeDiff.TotalMinutes:F1} min";
            }

            return null;
        }

        /// <summary>
        /// Kampüs Ağı Kontrolü
        /// Known campus IP ranges ile karşılaştırır.
        /// </summary>
        private string? ValidateCampusNetwork(string? clientIp)
        {
            if (string.IsNullOrEmpty(clientIp)) return null;

            // Known campus IP ranges (configurable)
            // Example: 10.0.0.0/8 (private), 192.168.0.0/16 (private), or specific campus ranges
            var campusRanges = new[]
            {
                "10.",          // Private network (campus VPN, WiFi)
                "192.168.",     // Private network (campus LAN)
                "172.16.",      // Private network range start
                "172.17.",
                "172.18.",
                "172.19.",
                "172.20.",
                "172.21.",
                "172.22.",
                "172.23.",
                "172.24.",
                "172.25.",
                "172.26.",
                "172.27.",
                "172.28.",
                "172.29.",
                "172.30.",
                "172.31.",
                "127.0.0.1",    // Localhost (development)
                "::1",           // IPv6 localhost
            };

            // Check if IP is in known campus ranges
            var isOnCampusNetwork = campusRanges.Any(range => clientIp.StartsWith(range));

            // For production, this should be more strict
            // For now, just log warning for public IPs
            if (!isOnCampusNetwork)
            {
                // Don't flag but note it - could be legitimate mobile data
                // Could make this configurable: strict mode vs lenient mode
                // return $"External network: {MaskIp(clientIp)}";
            }

            return null;
        }

        /// <summary>
        /// IP adresini maskeleyerek gizler (privacy)
        /// </summary>
        private string MaskIp(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.*.*";
            }
            return ip[..Math.Min(ip.Length, 10)] + "...";
        }

        // Backward compatibility wrapper
        private (bool IsFlagged, string? Reason) DetectSpoofing(CheckInRequestDto dto, AttendanceSession session, double distance, string? clientIp)
        {
            // Simple sync version for backward compat
            var flags = new List<string>();

            if (distance > (double)session.GeofenceRadius + 5.0)
                flags.Add($"Distance exceeded: {distance:F1}m");
            if (dto.Accuracy > 50)
                flags.Add($"Low accuracy: {dto.Accuracy:F1}m");
            if (dto.IsMockLocation == true)
                flags.Add("Mock location detected");

            return flags.Any() ? (true, string.Join("; ", flags)) : (false, null);
        }

        public async Task<List<MyAttendanceDto>> GetMyAttendanceAsync(int studentId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Where(e => e.StudentId == studentId && e.Status == "enrolled")
                .ToListAsync();

            var result = new List<MyAttendanceDto>();

            foreach (var enrollment in enrollments)
            {
                var totalSessions = await _context.AttendanceSessions
                    .CountAsync(s => s.SectionId == enrollment.SectionId);

                var attendedSessions = await _context.AttendanceRecords
                    .CountAsync(r => r.StudentId == studentId && 
                                     r.Session!.SectionId == enrollment.SectionId &&
                                     !r.IsFlagged);

                var excusedAbsences = await _context.ExcuseRequests
                    .CountAsync(er => er.StudentId == studentId &&
                                      er.Session!.SectionId == enrollment.SectionId &&
                                      er.Status == "approved");

                var percentage = totalSessions > 0 
                    ? Math.Round((decimal)(attendedSessions + excusedAbsences) / totalSessions * 100, 1) 
                    : 100;

                var status = percentage >= 70 ? "Good" : percentage >= 50 ? "Warning" : "Critical";

                result.Add(new MyAttendanceDto
                {
                    CourseId = enrollment.Section!.CourseId,
                    CourseCode = enrollment.Section.Course?.Code ?? "",
                    CourseName = enrollment.Section.Course?.Name ?? "",
                    TotalSessions = totalSessions,
                    AttendedSessions = attendedSessions,
                    ExcusedAbsences = excusedAbsences,
                    AttendancePercentage = percentage,
                    Status = status
                });
            }

            return result;
        }

        public async Task<AttendanceReportDto> GetAttendanceReportAsync(int sectionId)
        {
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null)
                throw new Exception("Section not found");

            var totalSessions = await _context.AttendanceSessions
                .CountAsync(s => s.SectionId == sectionId);

            var students = new List<StudentAttendanceDto>();

            foreach (var enrollment in section.Enrollments.Where(e => e.Status != "dropped"))
            {
                var attended = await _context.AttendanceRecords
                    .CountAsync(r => r.StudentId == enrollment.StudentId &&
                                     r.Session!.SectionId == sectionId &&
                                     !r.IsFlagged);

                var excused = await _context.ExcuseRequests
                    .CountAsync(er => er.StudentId == enrollment.StudentId &&
                                      er.Session!.SectionId == sectionId &&
                                      er.Status == "approved");

                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == enrollment.StudentId);

                var percentage = totalSessions > 0
                    ? Math.Round((decimal)(attended + excused) / totalSessions * 100, 1)
                    : 100;

                students.Add(new StudentAttendanceDto
                {
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student?.FirstName + " " + enrollment.Student?.LastName,
                    StudentNumber = student?.StudentNumber ?? "",
                    TotalSessions = totalSessions,
                    AttendedSessions = attended,
                    ExcusedAbsences = excused,
                    AttendancePercentage = percentage,
                    Status = percentage >= 70 ? "Good" : percentage >= 50 ? "Warning" : "Critical"
                });
            }

            return new AttendanceReportDto
            {
                Section = new CourseSectionDto
                {
                    Id = section.Id,
                    CourseId = section.CourseId,
                    CourseCode = section.Course?.Code ?? "",
                    CourseName = section.Course?.Name ?? "",
                    SectionNumber = section.SectionNumber,
                    Semester = section.Semester,
                    Year = section.Year,
                    InstructorId = section.InstructorId,
                    InstructorName = section.Instructor?.FirstName + " " + section.Instructor?.LastName
                },
                Students = students
            };
        }

        // ==================== EXCUSE REQUESTS ====================

        public async Task<ExcuseRequestDto> CreateExcuseRequestAsync(int studentId, CreateExcuseRequestDto dto, string? documentUrl)
        {
            var request = new ExcuseRequest
            {
                StudentId = studentId,
                SessionId = dto.SessionId,
                Reason = dto.Reason,
                DocumentUrl = documentUrl,
                Status = "pending"
            };

            _context.ExcuseRequests.Add(request);
            await _context.SaveChangesAsync();

            return await MapToExcuseRequestDtoAsync(request);
        }

        public async Task<List<ExcuseRequestDto>> GetExcuseRequestsAsync(int instructorId)
        {
            var requests = await _context.ExcuseRequests
                .Include(er => er.Student)
                .Include(er => er.Session)
                    .ThenInclude(s => s!.Section)
                .Include(er => er.Reviewer)
                .Where(er => er.Session!.InstructorId == instructorId)
                .OrderByDescending(er => er.CreatedAt)
                .ToListAsync();

            var result = new List<ExcuseRequestDto>();
            foreach (var r in requests)
            {
                result.Add(await MapToExcuseRequestDtoAsync(r));
            }
            return result;
        }

        public async Task<bool> ApproveExcuseAsync(int requestId, int reviewerId, string? notes)
        {
            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null) return false;

            request.Status = "approved";
            request.ReviewedBy = reviewerId;
            request.ReviewedAt = DateTime.UtcNow;
            request.Notes = notes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send notification
            if (_notificationService != null)
            {
                _ = _notificationService.SendExcuseApprovedAsync(request.StudentId, request.SessionId);
            }

            return true;
        }

        public async Task<bool> RejectExcuseAsync(int requestId, int reviewerId, string? notes)
        {
            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null) return false;

            request.Status = "rejected";
            request.ReviewedBy = reviewerId;
            request.ReviewedAt = DateTime.UtcNow;
            request.Notes = notes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send notification
            if (_notificationService != null)
            {
                _ = _notificationService.SendExcuseRejectedAsync(request.StudentId, request.SessionId, notes);
            }

            return true;
        }

        public string GenerateQrCode()
        {
            return Guid.NewGuid().ToString("N")[..16].ToUpper();
        }

        // ==================== MAPPERS ====================

        private async Task<AttendanceSessionDto> MapToSessionDtoAsync(AttendanceSession session)
        {
            var totalStudents = await _context.Enrollments
                .CountAsync(e => e.SectionId == session.SectionId && e.Status == "enrolled");

            return new AttendanceSessionDto
            {
                Id = session.Id,
                SectionId = session.SectionId,
                CourseCode = session.Section?.Course?.Code ?? "",
                CourseName = session.Section?.Course?.Name ?? "",
                SectionNumber = session.Section?.SectionNumber ?? "",
                InstructorId = session.InstructorId,
                InstructorName = session.Instructor?.FirstName + " " + session.Instructor?.LastName,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Latitude = session.Latitude,
                Longitude = session.Longitude,
                GeofenceRadius = session.GeofenceRadius,
                QrCode = session.QrCode,
                QrCodeExpiry = session.QrCodeExpiry,
                Status = session.Status,
                AttendedCount = session.Records?.Count ?? 0,
                TotalStudents = totalStudents
            };
        }

        private async Task<ExcuseRequestDto> MapToExcuseRequestDtoAsync(ExcuseRequest request)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId);

            return new ExcuseRequestDto
            {
                Id = request.Id,
                StudentId = request.StudentId,
                StudentName = request.Student?.FirstName + " " + request.Student?.LastName,
                SessionId = request.SessionId,
                Session = session == null ? null : await MapToSessionDtoAsync(session),
                Reason = request.Reason,
                DocumentUrl = request.DocumentUrl,
                Status = request.Status,
                ReviewedBy = request.ReviewedBy,
                ReviewerName = request.Reviewer?.FirstName + " " + request.Reviewer?.LastName,
                ReviewedAt = request.ReviewedAt,
                Notes = request.Notes,
                CreatedAt = request.CreatedAt
            };
        }
    }
}
