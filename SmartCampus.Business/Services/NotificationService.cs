using SmartCampus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartCampus.Business.Services
{
    public interface INotificationService
    {
        Task SendEnrollmentConfirmationAsync(int studentId, int sectionId);
        Task SendGradeNotificationAsync(int studentId, int enrollmentId);
        Task SendSessionStartNotificationAsync(int sectionId, int sessionId);
        Task SendExcuseApprovedAsync(int studentId, int sessionId);
        Task SendExcuseRejectedAsync(int studentId, int sessionId, string? notes);
    }

    /// <summary>
    /// Bildirim servisi - Email bildirimleri gönderir
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly CampusDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            CampusDbContext context,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Ders kaydı onay bildirimi
        /// </summary>
        public async Task SendEnrollmentConfirmationAsync(int studentId, int sectionId)
        {
            try
            {
                var student = await _context.Users.FindAsync(studentId);
                var section = await _context.CourseSections
                    .Include(s => s.Course)
                    .Include(s => s.Instructor)
                    .FirstOrDefaultAsync(s => s.Id == sectionId);

                if (student == null || section == null) return;

                var subject = $"✅ Ders Kaydı Onaylandı - {section.Course?.Code}";
                var body = $@"
Sayın {student.FirstName} {student.LastName},

Aşağıdaki derse kaydınız başarıyla gerçekleştirilmiştir:

📚 Ders: {section.Course?.Code} - {section.Course?.Name}
👤 Öğretim Üyesi: {section.Instructor?.FirstName} {section.Instructor?.LastName}
📅 Dönem: {section.Semester} {section.Year}
🔢 Section: {section.SectionNumber}

Derslerinizde başarılar dileriz.

Saygılarımızla,
Smart Campus Akademik Sistem
";

                await _emailService.SendEmailAsync(student.Email, subject, body);
                _logger.LogInformation($"📧 Kayıt bildirimi gönderildi: {student.Email} - {section.Course?.Code}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Kayıt bildirimi gönderilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Not girişi bildirimi
        /// </summary>
        public async Task SendGradeNotificationAsync(int studentId, int enrollmentId)
        {
            try
            {
                var student = await _context.Users.FindAsync(studentId);
                var enrollment = await _context.Enrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                if (student == null || enrollment == null) return;

                var courseName = enrollment.Section?.Course?.Name ?? "Bilinmiyor";
                var courseCode = enrollment.Section?.Course?.Code ?? "";

                var subject = $"📊 Not Girişi Yapıldı - {courseCode}";
                var body = $@"
Sayın {student.FirstName} {student.LastName},

{courseCode} - {courseName} dersi için not girişi yapılmıştır.

📝 Vize: {(enrollment.MidtermGrade?.ToString("F1") ?? "-")}
📝 Final: {(enrollment.FinalGrade?.ToString("F1") ?? "-")}
📝 Ödev: {(enrollment.HomeworkGrade?.ToString("F1") ?? "-")}
🎯 Harf Notu: {enrollment.LetterGrade ?? "-"}

Not detaylarını Smart Campus sisteminden görüntüleyebilirsiniz.

Saygılarımızla,
Smart Campus Akademik Sistem
";

                await _emailService.SendEmailAsync(student.Email, subject, body);
                _logger.LogInformation($"📧 Not bildirimi gönderildi: {student.Email} - {courseCode}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Not bildirimi gönderilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Yoklama oturumu başladı bildirimi
        /// </summary>
        public async Task SendSessionStartNotificationAsync(int sectionId, int sessionId)
        {
            try
            {
                var session = await _context.AttendanceSessions
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Course)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null) return;

                // Bu derse kayıtlı öğrencileri al
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.SectionId == sectionId && e.Status == "Active")
                    .Select(e => e.StudentId)
                    .ToListAsync();

                var students = await _context.Users
                    .Where(u => enrolledStudentIds.Contains(u.Id))
                    .ToListAsync();

                var courseName = session.Section?.Course?.Name ?? "Bilinmiyor";
                var courseCode = session.Section?.Course?.Code ?? "";

                foreach (var student in students)
                {
                    var subject = $"🔔 Yoklama Açıldı - {courseCode}";
                    var body = $@"
Sayın {student.FirstName} {student.LastName},

{courseCode} - {courseName} dersi için yoklama açılmıştır.

📅 Tarih: {session.Date:dd.MM.yyyy}
⏰ Süre: {session.StartTime:HH:mm} - {session.EndTime:HH:mm}

Lütfen yoklamanızı vermeyi unutmayın!

Saygılarımızla,
Smart Campus Akademik Sistem
";

                    try
                    {
                        await _emailService.SendEmailAsync(student.Email, subject, body);
                    }
                    catch
                    {
                        // Individual email failure shouldn't stop others
                    }
                }

                _logger.LogInformation($"📧 Yoklama bildirimi gönderildi: {students.Count} öğrenci - {courseCode}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Yoklama bildirimi gönderilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Mazeret onaylandı bildirimi
        /// </summary>
        public async Task SendExcuseApprovedAsync(int studentId, int sessionId)
        {
            try
            {
                var student = await _context.Users.FindAsync(studentId);
                var session = await _context.AttendanceSessions
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Course)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (student == null || session == null) return;

                var courseName = session.Section?.Course?.Name ?? "Bilinmiyor";

                var subject = "✅ Mazeret Talebiniz Onaylandı";
                var body = $@"
Sayın {student.FirstName} {student.LastName},

{session.Date:dd.MM.yyyy} tarihli {courseName} dersi için vermiş olduğunuz mazeret talebiniz onaylanmıştır.

Bu devamsızlık mazeretli olarak kaydedilmiştir.

Saygılarımızla,
Smart Campus Akademik Sistem
";

                await _emailService.SendEmailAsync(student.Email, subject, body);
                _logger.LogInformation($"📧 Mazeret onay bildirimi gönderildi: {student.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Mazeret onay bildirimi gönderilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Mazeret reddedildi bildirimi
        /// </summary>
        public async Task SendExcuseRejectedAsync(int studentId, int sessionId, string? notes)
        {
            try
            {
                var student = await _context.Users.FindAsync(studentId);
                var session = await _context.AttendanceSessions
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Course)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (student == null || session == null) return;

                var courseName = session.Section?.Course?.Name ?? "Bilinmiyor";

                var subject = "❌ Mazeret Talebiniz Reddedildi";
                var body = $@"
Sayın {student.FirstName} {student.LastName},

{session.Date:dd.MM.yyyy} tarihli {courseName} dersi için vermiş olduğunuz mazeret talebiniz reddedilmiştir.

{(string.IsNullOrEmpty(notes) ? "" : $"Açıklama: {notes}")}

Sorularınız için ilgili öğretim üyesi ile iletişime geçebilirsiniz.

Saygılarımızla,
Smart Campus Akademik Sistem
";

                await _emailService.SendEmailAsync(student.Email, subject, body);
                _logger.LogInformation($"📧 Mazeret red bildirimi gönderildi: {student.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Mazeret red bildirimi gönderilemedi: {ex.Message}");
            }
        }
    }
}
