using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartCampus.Business.Services
{
    public interface INotificationService
    {
        // General Purpose Methods
        Task SendNotificationAsync(int userId, string title, string message, string type, string? refType = null, string? refId = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int notificationId, int userId);

        // Specialized Business Methods
        Task SendEnrollmentConfirmationAsync(int studentId, int sectionId);
        Task SendGradeNotificationAsync(int studentId, int enrollmentId);
        Task SendSessionStartNotificationAsync(int sectionId, int sessionId);
        Task SendExcuseApprovedAsync(int studentId, int sessionId);
        Task SendExcuseRejectedAsync(int studentId, int sessionId, string? notes);
    }

    /// <summary>
    /// Bildirim servisi - Database, SignalR ve Email bildirimleri yönetir
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly CampusDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationHubService _hubService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            CampusDbContext context,
            IEmailService emailService,
            INotificationHubService hubService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _hubService = hubService;
            _logger = logger;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type, string? refType = null, string? refId = null)
        {
            try
            {
                // 1. Save to Database
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    ReferenceType = refType,
                    ReferenceId = refId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 2. Send via SignalR (Real-time)
                await _hubService.SendNotificationToUserAsync(userId.ToString(), new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    createdAt = notification.CreatedAt,
                    isRead = false
                });

                // 3. Send via Email (Check preferences)
                await CheckAndSendEmailAsync(userId, title, message, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to user {userId}");
            }
        }

        private async Task CheckAndSendEmailAsync(int userId, string title, string message, string type)
        {
            try
            {
                // Check preferences
                var prefs = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                
                // Default to true if no prefs
                bool emailEnabled = prefs?.EmailEnabled ?? true;
                
                // Check granular if needed (assuming generic mapping for now)
                if (type == "Academic" && prefs != null && !prefs.AcademicNotifications) emailEnabled = false;
                if (type == "Attendance" && prefs != null && !prefs.AttendanceNotifications) emailEnabled = false;

                if (!emailEnabled) return;

                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(user.Email, title, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email sending failed for user {userId}: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var n in unread) n.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteNotificationAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsDeleted = true; // Soft delete
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // Refactored Specialized Methods
        // ==========================================

        public async Task SendEnrollmentConfirmationAsync(int studentId, int sectionId)
        {
            try
            {
                var section = await _context.CourseSections
                    .Include(s => s.Course)
                    .Include(s => s.Instructor)
                    .FirstOrDefaultAsync(s => s.Id == sectionId);

                if (section == null)
                {
                    _logger.LogWarning($"Section {sectionId} not found for enrollment confirmation");
                    return;
                }

                if (section.Course == null)
                {
                    _logger.LogWarning($"Section {sectionId} has no Course");
                    return;
                }

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning($"Student {studentId} not found for enrollment confirmation");
                    return;
                }
                
                var courseCode = section.Course.Code ?? "N/A";
                var courseName = section.Course.Name ?? "Ders";
                
                var title = $"✅ Ders Kaydı Onaylandı - {courseCode}";
                var message = $"Sayın Öğrenci, {courseCode} - {courseName} dersine kaydınız onaylanmıştır.";

                await SendNotificationAsync(student.UserId, title, message, "Academic", "Enrollment", sectionId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending enrollment confirmation for section {sectionId}, student {studentId}");
                // Don't throw - notification failure shouldn't break enrollment
            }
        }

        public async Task SendGradeNotificationAsync(int studentId, int enrollmentId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s!.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                // Comprehensive null checks
                if (enrollment == null)
                {
                    _logger.LogWarning($"Enrollment {enrollmentId} not found for grade notification");
                    return;
                }

                if (enrollment.Section == null)
                {
                    _logger.LogWarning($"Enrollment {enrollmentId} has no Section");
                    return;
                }

                if (enrollment.Section.Course == null)
                {
                    _logger.LogWarning($"Enrollment {enrollmentId} Section {enrollment.Section.Id} has no Course");
                    return;
                }

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning($"Student {studentId} not found for grade notification");
                    return;
                }

                var courseCode = enrollment.Section.Course.Code ?? "N/A";
                var courseName = enrollment.Section.Course.Name ?? "Ders";
                
                var title = $"📊 Not Girişi - {courseCode}";
                var message = $"{courseName} dersi notlarınız güncellendi.";

                await SendNotificationAsync(student.UserId, title, message, "Academic", "Grade", enrollmentId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending grade notification for enrollment {enrollmentId}, student {studentId}");
                // Don't throw - notification failure shouldn't break grade entry
            }
        }

        public async Task SendSessionStartNotificationAsync(int sectionId, int sessionId)
        {
            try
            {
                var session = await _context.AttendanceSessions
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    _logger.LogWarning($"AttendanceSession {sessionId} not found");
                    return;
                }

                if (session.Section == null)
                {
                    _logger.LogWarning($"AttendanceSession {sessionId} has no Section");
                    return;
                }

                if (session.Section.Course == null)
                {
                    _logger.LogWarning($"AttendanceSession {sessionId} Section {session.Section.Id} has no Course");
                    return;
                }

                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.SectionId == sectionId && e.Status == "enrolled")
                    .Select(e => e.StudentId)
                    .ToListAsync();

                if (!enrolledStudentIds.Any())
                {
                    _logger.LogInformation($"No enrolled students found for section {sectionId}");
                    return;
                }

                var userIds = await _context.Students
                    .Where(s => enrolledStudentIds.Contains(s.Id))
                    .Select(s => s.UserId)
                    .ToListAsync();

                var courseCode = session.Section.Course.Code ?? "N/A";
                var courseName = session.Section.Course.Name ?? "Ders";

                var title = $"🔔 Yoklama - {courseCode}";
                var message = $"{courseName} dersi için yoklama başlamıştır.";

                foreach (var userId in userIds)
                {
                    // Parallel execution or simpler loop
                    await SendNotificationAsync(userId, title, message, "Attendance", "Session", sessionId.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending session start notification for section {sectionId}, session {sessionId}");
                // Don't throw - notification failure shouldn't break attendance
            }
        }

        public async Task SendExcuseApprovedAsync(int studentId, int sessionId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning($"Student {studentId} not found for excuse approval notification");
                    return;
                }

                var session = await _context.AttendanceSessions.FindAsync(sessionId);
                var sessionDate = session?.Date.ToString("dd.MM.yyyy") ?? "tarih belirtilmemiş";
                
                await SendNotificationAsync(student.UserId, "✅ Mazeret Onaylandı", 
                    $"{sessionDate} tarihli ders için mazeretiniz onaylandı.", 
                    "Attendance", "Excuse", sessionId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending excuse approval notification for student {studentId}, session {sessionId}");
                // Don't throw - notification failure shouldn't break excuse approval
            }
        }

        public async Task SendExcuseRejectedAsync(int studentId, int sessionId, string? notes)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning($"Student {studentId} not found for excuse rejection notification");
                    return;
                }

                var session = await _context.AttendanceSessions.FindAsync(sessionId);
                var sessionDate = session?.Date.ToString("dd.MM.yyyy") ?? "tarih belirtilmemiş";
                var notesText = string.IsNullOrEmpty(notes) ? "" : $" {notes}";

                await SendNotificationAsync(student.UserId, "❌ Mazeret Reddedildi", 
                    $"{sessionDate} tarihli ders için mazeretiniz reddedildi.{notesText}", 
                    "Attendance", "Excuse", sessionId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending excuse rejection notification for student {studentId}, session {sessionId}");
                // Don't throw - notification failure shouldn't break excuse rejection
            }
        }
    }
}
