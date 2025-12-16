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
        Task<List<AttendanceSessionDto>> GetActiveSessionsForStudentAsync(int studentId);
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
            // Önce section'ın var olup olmadığını kontrol et
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == dto.SectionId && !s.IsDeleted);

            if (section == null)
                throw new Exception($"Section bulunamadı (ID: {dto.SectionId})");

            // Instructor kontrolü
            if (section.InstructorId != instructorId)
                throw new UnauthorizedAccessException($"Bu section'a yoklama başlatma yetkiniz yok. Section'ın öğretmeni: {section.InstructorId}, Sizin ID'niz: {instructorId}");

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

            // Calculate total students BEFORE SaveChanges to avoid DbContext concurrency issues
            var totalStudents = await _context.Enrollments
                .CountAsync(e => e.SectionId == dto.SectionId && e.Status == "enrolled");

            // Attach section to session before saving (to avoid loading it again)
            session.Section = section;
            
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Send notification to enrolled students (fire and forget, don't await)
            if (_notificationService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendSessionStartNotificationAsync(dto.SectionId, session.Id);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });
            }

            // Map to DTO - section is already attached, totalStudents is pre-calculated
            return await MapToSessionDtoAsync(session, totalStudents);
        }

        public async Task<AttendanceSessionDto?> GetSessionByIdAsync(int id)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Classroom)
                .Include(s => s.Instructor)
                .Include(s => s.Records)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return null;

            // Calculate totalStudents before mapping to avoid concurrency issues
            var totalStudents = await _context.Enrollments
                .CountAsync(e => e.SectionId == session.SectionId && e.Status == "enrolled");

            return await MapToSessionDtoAsync(session, totalStudents);
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
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Classroom)
                .Include(s => s.Records)
                .Where(s => s.InstructorId == instructorId)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            // Pre-calculate totalStudents for all sections to avoid multiple queries
            var sectionIds = sessions.Select(s => s.SectionId).Distinct().ToList();
            var studentCounts = await _context.Enrollments
                .Where(e => sectionIds.Contains(e.SectionId) && e.Status == "enrolled")
                .GroupBy(e => e.SectionId)
                .Select(g => new { SectionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SectionId, x => x.Count);

            var result = new List<AttendanceSessionDto>();
            foreach (var s in sessions)
            {
                var totalStudents = studentCounts.GetValueOrDefault(s.SectionId, 0);
                result.Add(await MapToSessionDtoAsync(s, totalStudents));
            }
            return result;
        }

        public async Task<List<AttendanceSessionDto>> GetActiveSessionsForStudentAsync(int studentId)
        {
            Console.WriteLine($"\n🔍 GetActiveSessionsForStudentAsync called with studentId (userId): {studentId}");
            
            // GetMyCoursesAsync gibi direkt studentId (userId) ile enrollment'ları bul
            // Önce Student entity'sini bul
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId);
            
            if (student == null)
            {
                Console.WriteLine($"❌ Student not found for userId: {studentId}");
                return new List<AttendanceSessionDto>();
            }

            Console.WriteLine($"✅ Student found: Id={student.Id}, UserId={student.UserId}");

            // GetMyCoursesAsync ile aynı mantık: studentId (Student entity Id) ile enrollment'ları bul
            // Debug: Önce tüm enrollment'ları göster (status kontrolü olmadan)
            var allEnrollmentsDebug = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Where(e => e.StudentId == student.Id)
                .ToListAsync();
            Console.WriteLine($"📋 All enrollments for student {student.Id} (StudentId, any status): {allEnrollmentsDebug.Count}");
            foreach (var e in allEnrollmentsDebug)
            {
                Console.WriteLine($"  - Enrollment Id={e.Id}: SectionId={e.SectionId}, CourseId={e.Section?.CourseId}, Status='{e.Status}', StudentId={e.StudentId}");
            }
            
            // GetMyCoursesAsync gibi: "enrolled" status'undaki enrollment'ları al
            var enrolledSectionIds = await _context.Enrollments
                .Where(e => e.StudentId == student.Id && e.Status == "enrolled")
                .Select(e => e.SectionId)
                .ToListAsync();

            Console.WriteLine($"✅ Student {student.Id} enrolled in {enrolledSectionIds.Count} sections (status='enrolled'): [{string.Join(", ", enrolledSectionIds)}]");

            if (!enrolledSectionIds.Any())
            {
                Console.WriteLine("⚠️ Student has no enrolled sections (status='enrolled')");
                // Eğer hiç enrollment yoksa, tüm enrollment'ları tekrar kontrol et
                var anyEnrollments = await _context.Enrollments
                    .Where(e => e.StudentId == student.Id)
                    .AnyAsync();
                if (anyEnrollments)
                {
                    Console.WriteLine("⚠️ But student HAS enrollments with different status! Check logs above.");
                }
                return new List<AttendanceSessionDto>();
            }

            // Öğrencinin kayıtlı olduğu section'ların course'larını bul
            var enrolledCourseIds = await _context.CourseSections
                .Where(s => enrolledSectionIds.Contains(s.Id))
                .Select(s => s.CourseId)
                .Distinct()
                .ToListAsync();

            Console.WriteLine($"📚 Student enrolled in {enrolledCourseIds.Count} courses: [{string.Join(", ", enrolledCourseIds)}]");
            
            // Debug: Her section'ın course'ını göster
            var sectionsWithCourses = await _context.CourseSections
                .Where(s => enrolledSectionIds.Contains(s.Id))
                .Select(s => new { SectionId = s.Id, CourseId = s.CourseId })
                .ToListAsync();
            Console.WriteLine($"📋 Enrolled sections with courses:");
            foreach (var sc in sectionsWithCourses)
            {
                Console.WriteLine($"  - Section {sc.SectionId} -> Course {sc.CourseId}");
            }

            // Bu course'ların TÜM section'larını bul (öğrenci hangi section'a kayıtlı olursa olsun, o dersin tüm section'larındaki oturumları görebilmeli)
            var allSectionsForEnrolledCourses = await _context.CourseSections
                .Where(s => enrolledCourseIds.Contains(s.CourseId))
                .Select(s => s.Id)
                .ToListAsync();

            Console.WriteLine($"📋 All sections for enrolled courses ({enrolledCourseIds.Count} courses): {allSectionsForEnrolledCourses.Count} sections");
            Console.WriteLine($"📋 Section IDs: [{string.Join(", ", allSectionsForEnrolledCourses)}]");

            var now = DateTime.UtcNow;
            var today = now.Date;
            Console.WriteLine($"🕐 Current UTC time: {now:yyyy-MM-dd HH:mm:ss}, Today: {today:yyyy-MM-dd}");

            // Debug: Tüm aktif session'ları göster (tarih filtresi olmadan)
            var allSessionsAnyStatus = await _context.AttendanceSessions
                .ToListAsync();
            Console.WriteLine($"🔍 All sessions in database (any status): {allSessionsAnyStatus.Count}");
            foreach (var s in allSessionsAnyStatus)
            {
                Console.WriteLine($"  - Session {s.Id}: SectionId={s.SectionId}, Date={s.Date:yyyy-MM-dd}, Status='{s.Status}', Date >= today: {s.Date.Date >= today}");
            }

            // Debug: Tüm aktif session'ları göster
            var allActiveSessions = await _context.AttendanceSessions
                .Where(s => (s.Status.ToLower() == "active" || s.Status == "Active") && s.Date.Date >= today)
                .ToListAsync();
            Console.WriteLine($"🔍 All active sessions in database (status='active' AND date >= today): {allActiveSessions.Count}");
            foreach (var s in allActiveSessions)
            {
                Console.WriteLine($"  - Session {s.Id}: SectionId={s.SectionId}, Date={s.Date:yyyy-MM-dd}, Status='{s.Status}', IsEnrolledCourse: {allSectionsForEnrolledCourses.Contains(s.SectionId)}");
            }

            // Get active sessions for ALL sections of enrolled courses
            // Öğrenci derse kayıtlıysa, o dersin tüm section'larındaki aktif oturumları görebilmeli
            var sessions = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Classroom)
                .Include(s => s.Records)
                .Where(s => allSectionsForEnrolledCourses.Contains(s.SectionId) && 
                           (s.Status.ToLower() == "active" || s.Status == "Active") &&
                           s.Date.Date >= today) // Bugün veya gelecek tarih
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            Console.WriteLine($"✅ Found {sessions.Count} active sessions for student {student.Id}");
            foreach (var s in sessions)
            {
                Console.WriteLine($"  - Session {s.Id}: Section {s.SectionId}, Date: {s.Date:yyyy-MM-dd}, Time: {s.StartTime} - {s.EndTime}, Status: '{s.Status}'");
            }

            // Pre-calculate totalStudents for all sections to avoid multiple queries
            var sectionIds = sessions.Select(s => s.SectionId).Distinct().ToList();
            var studentCounts = await _context.Enrollments
                .Where(e => sectionIds.Contains(e.SectionId) && e.Status == "enrolled")
                .GroupBy(e => e.SectionId)
                .Select(g => new { SectionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SectionId, x => x.Count);

            var result = new List<AttendanceSessionDto>();
            foreach (var s in sessions)
            {
                var totalStudents = studentCounts.GetValueOrDefault(s.SectionId, 0);
                result.Add(await MapToSessionDtoAsync(s, totalStudents));
            }
            return result;
        }

        public async Task<CheckInResponseDto> CheckInAsync(int sessionId, int studentId, CheckInRequestDto dto, string? clientIp)
        {
            // Check if student is active
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == studentId);
            
            if (student == null)
            {
                throw new InvalidOperationException("Öğrenci bulunamadı.");
            }
            
            if (!student.IsActive)
            {
                throw new InvalidOperationException("Pasif öğrenciler yoklama veremez.");
            }
            
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

            // 2. Accuracy check - Device type aware (mobile vs desktop)
            // Mobile devices have GPS hardware: expect < 50m accuracy
            // Desktop devices use IP/WiFi location: expect < 500m accuracy
            var accuracyThreshold = dto.DeviceType == "mobile" ? 50m : 500m;
            if (dto.Accuracy > accuracyThreshold)
            {
                flags.Add($"Low accuracy for {dto.DeviceType ?? "unknown"} device: {dto.Accuracy:F1}m (threshold: {accuracyThreshold}m)");
            }
            
            // 2a. Mobile-specific: Very low accuracy is suspicious (should have GPS hardware)
            if (dto.DeviceType == "mobile" && dto.Accuracy > 100)
            {
                flags.Add($"Suspiciously low accuracy for mobile device: {dto.Accuracy:F1}m (possible IP-based location)");
            }
            
            // 2b. Desktop-specific: Very high accuracy is suspicious (should use IP/WiFi)
            if (dto.DeviceType == "desktop" && dto.Accuracy < 10)
            {
                flags.Add($"Suspiciously high accuracy for desktop: {dto.Accuracy:F1}m (possible GPS spoofing)");
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
                    SectionId = enrollment.SectionId,
                    CourseCode = enrollment.Section.Course?.Code ?? "",
                    CourseName = enrollment.Section.Course?.Name ?? "",
                    SectionNumber = enrollment.Section.SectionNumber,
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

        private async Task<AttendanceSessionDto> MapToSessionDtoAsync(AttendanceSession session, int? totalStudents = null)
        {
            // If totalStudents not provided, calculate it (but try to avoid this to prevent concurrency issues)
            int studentCount = totalStudents ?? 0;
            if (totalStudents == null)
            {
                try
                {
                    studentCount = await _context.Enrollments
                        .CountAsync(e => e.SectionId == session.SectionId && e.Status == "enrolled");
                }
                catch
                {
                    studentCount = 0;
                }
            }

            // Section should already be loaded with navigation properties by the caller
            // Don't reload it here to avoid DbContext concurrency issues

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
                TotalStudents = studentCount,
                Classroom = session.Section?.Classroom == null ? null : new ClassroomDto
                {
                    Id = session.Section.Classroom.Id,
                    Building = session.Section.Classroom.Building,
                    RoomNumber = session.Section.Classroom.RoomNumber,
                    Capacity = session.Section.Classroom.Capacity,
                    Latitude = session.Section.Classroom.Latitude,
                    Longitude = session.Section.Classroom.Longitude,
                    FeaturesJson = session.Section.Classroom.FeaturesJson
                },
                Section = session.Section == null ? null : new CourseSectionDto
                {
                    Id = session.Section.Id,
                    CourseId = session.Section.CourseId,
                    CourseCode = session.Section.Course?.Code ?? "",
                    CourseName = session.Section.Course?.Name ?? "",
                    SectionNumber = session.Section.SectionNumber,
                    Semester = session.Section.Semester,
                    Year = session.Section.Year,
                    InstructorId = session.Section.InstructorId,
                    InstructorName = session.Section.Instructor?.FirstName + " " + session.Section.Instructor?.LastName
                }
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
