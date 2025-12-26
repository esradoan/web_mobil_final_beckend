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
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null) return;

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;
            
            var title = $"✅ Ders Kaydı Onaylandı - {section.Course?.Code}";
            var message = $"Sayın Öğrenci, {section.Course?.Code} - {section.Course?.Name} dersine kaydınız onaylanmıştır.";

            await SendNotificationAsync(student.UserId, title, message, "Academic", "Enrollment", sectionId.ToString());
        }

        public async Task SendGradeNotificationAsync(int studentId, int enrollmentId)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment?.Section == null) return;

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var title = $"📊 Not Girişi - {enrollment.Section.Course?.Code}";
            var message = $"{enrollment.Section.Course?.Name} dersi notlarınız güncellendi.";

            await SendNotificationAsync(student.UserId, title, message, "Academic", "Grade", enrollmentId.ToString());
        }

        public async Task SendSessionStartNotificationAsync(int sectionId, int sessionId)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Course)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session?.Section == null) return;

            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.SectionId == sectionId && e.Status == "enrolled")
                .Select(e => e.StudentId)
                .ToListAsync();

            var userIds = await _context.Students
                .Where(s => enrolledStudentIds.Contains(s.Id))
                .Select(s => s.UserId)
                .ToListAsync();

            var title = $"🔔 Yoklama - {session.Section.Course?.Code}";
            var message = $"{session.Section.Course?.Name} dersi için yoklama başlamıştır.";

            foreach (var userId in userIds)
            {
                // Parallel execution or simpler loop
                await SendNotificationAsync(userId, title, message, "Attendance", "Session", sessionId.ToString());
            }
        }

        public async Task SendExcuseApprovedAsync(int studentId, int sessionId)
        {
             var student = await _context.Students.FindAsync(studentId);
             if (student == null) return;

             var session = await _context.AttendanceSessions.FindAsync(sessionId);
             
             await SendNotificationAsync(student.UserId, "✅ Mazeret Onaylandı", 
                 $"{session?.Date:dd.MM.yyyy} tarihli ders için mazeretiniz onaylandı.", 
                 "Attendance", "Excuse", sessionId.ToString());
        }

        public async Task SendExcuseRejectedAsync(int studentId, int sessionId, string? notes)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var session = await _context.AttendanceSessions.FindAsync(sessionId);

            await SendNotificationAsync(student.UserId, "❌ Mazeret Reddedildi", 
                $"{session?.Date:dd.MM.yyyy} tarihli ders için mazeretiniz reddedildi. {notes}", 
                "Attendance", "Excuse", sessionId.ToString());
        }
    }
}
