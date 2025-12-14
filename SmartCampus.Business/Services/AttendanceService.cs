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
        private const double EarthRadiusMeters = 6371000;

        public AttendanceService(CampusDbContext context)
        {
            _context = context;
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

            // Spoofing detection
            var (isFlagged, flagReason) = DetectSpoofing(dto, session, distance, clientIp);

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

        private (bool IsFlagged, string? Reason) DetectSpoofing(CheckInRequestDto dto, AttendanceSession session, double distance, string? clientIp)
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

            // 3. IP check (kampüs ağı - placeholder)
            // In production, check if IP is in campus range
            // For now, just log the IP
            // if (!IsCampusNetwork(clientIp)) flags.Add("Not on campus network");

            if (flags.Any())
            {
                return (true, string.Join("; ", flags));
            }

            return (false, null);
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
