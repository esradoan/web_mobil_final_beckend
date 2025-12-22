using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartCampus.DataAccess;

namespace SmartCampus.Business.Services
{
    /// <summary>
    /// Günlük çalışan devamsızlık uyarı servisi
    /// - >= 20% devamsızlık: Warning email
    /// - >= 30% devamsızlık: Critical warning email + danışman bildirimi
    /// </summary>
    public class AbsenceWarningService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AbsenceWarningService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Her 24 saatte bir
        
        public AbsenceWarningService(
            IServiceProvider serviceProvider,
            ILogger<AbsenceWarningService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📊 Absence Warning Service başlatıldı");

            // İlk çalıştırmada biraz bekle (uygulama başlangıcı için)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAbsenceRatesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Devamsızlık kontrolü sırasında hata oluştu");
                }

                // Sonraki kontrole kadar bekle
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAbsenceRatesAsync()
        {
            _logger.LogInformation("🔍 Devamsızlık oranları kontrol ediliyor...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CampusDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Aktif dönemdeki tüm enrollment'ları al
            var currentSemester = GetCurrentSemester();
            var currentYear = DateTime.Now.Year;

            var enrollments = await context.Enrollments
                .Include(e => e.Student) // Student is actually User type
                .Include(e => e.Section)
                    .ThenInclude(s => s.Course)
                .Where(e => e.Status == "Active" && 
                           e.Section != null &&
                           e.Section.Semester == currentSemester &&
                           e.Section.Year == currentYear)
                .ToListAsync();

            var warningCount = 0;
            var criticalCount = 0;

            foreach (var enrollment in enrollments)
            {
                // Bu enrollment için yoklama istatistiklerini hesapla
                if (enrollment.Section == null) continue;
                
                var sectionId = enrollment.SectionId;
                var studentId = enrollment.StudentId;

                var totalSessions = await context.AttendanceSessions
                    .CountAsync(s => s.SectionId == sectionId && s.Status == "Closed");

                if (totalSessions == 0) continue; // Henüz yoklama yok

                var attendedSessions = await context.AttendanceRecords
                    .Include(r => r.Session)
                    .CountAsync(r => r.Session != null &&
                                    r.Session.SectionId == sectionId && 
                                    r.StudentId == studentId &&
                                    !r.IsFlagged);

                var excusedAbsences = await context.ExcuseRequests
                    .Include(e => e.Session)
                    .CountAsync(e => e.StudentId == studentId &&
                                    e.Session != null &&
                                    e.Session.SectionId == sectionId &&
                                    e.Status == "Approved");

                var effectiveAttended = attendedSessions + excusedAbsences;
                var absenceRate = 100.0 - ((double)effectiveAttended / totalSessions * 100);

                // Uyarı eşiklerini kontrol et
                if (absenceRate >= 30)
                {
                    // Kritik uyarı
                    await SendCriticalWarningAsync(enrollment, absenceRate, emailService);
                    criticalCount++;
                }
                else if (absenceRate >= 20)
                {
                    // Normal uyarı
                    await SendWarningAsync(enrollment, absenceRate, emailService);
                    warningCount++;
                }
            }

            _logger.LogInformation($"✅ Devamsızlık kontrolü tamamlandı. Uyarı: {warningCount}, Kritik: {criticalCount}");
        }

        private async Task SendWarningAsync(dynamic enrollment, double absenceRate, IEmailService emailService)
        {
            var studentEmail = enrollment.Student?.Email as string;
            var studentName = (enrollment.Student?.FirstName as string ?? "") + " " + (enrollment.Student?.LastName as string ?? "");
            var courseName = enrollment.Section?.Course?.Name as string;

            if (string.IsNullOrEmpty(studentEmail)) return;

            var subject = $"⚠️ Devamsızlık Uyarısı - {courseName}";
            var body = $@"
Sayın {studentName},

{courseName} dersindeki devamsızlık oranınız %{absenceRate:F1} seviyesine ulaşmıştır.

ℹ️ Uyarı Eşiği: %20
⚠️ Kritik Eşik: %30 (dersten başarısız sayılırsınız)

Lütfen derslerinize düzenli olarak katılım sağlayınız.

Saygılarımızla,
Smart Campus Akademik Sistem
";

            try
            {
                await emailService.SendEmailAsync(studentEmail, subject, body);
                _logger.LogInformation($"📧 Uyarı gönderildi: {studentEmail} - {courseName} ({absenceRate:F1}%)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email gönderilemedi: {studentEmail} - {ex.Message}");
            }
        }

        private async Task SendCriticalWarningAsync(dynamic enrollment, double absenceRate, IEmailService emailService)
        {
            var studentEmail = enrollment.Student?.Email as string;
            var studentName = (enrollment.Student?.FirstName as string ?? "") + " " + (enrollment.Student?.LastName as string ?? "");
            var courseName = enrollment.Section?.Course?.Name as string;

            if (string.IsNullOrEmpty(studentEmail)) return;

            var subject = $"🚨 KRİTİK Devamsızlık Uyarısı - {courseName}";
            var body = $@"
Sayın {studentName},

{courseName} dersindeki devamsızlık oranınız %{absenceRate:F1} seviyesine ulaşmıştır.

🚨 KRİTİK DURUM: Devamsızlık sınırını aştınız!

Mevcut durumda bu dersten devamsızlık nedeniyle başarısız sayılma riskiniz bulunmaktadır.

Lütfen ilgili öğretim üyesi veya danışmanınız ile iletişime geçiniz.

Saygılarımızla,
Smart Campus Akademik Sistem
";

            try
            {
                await emailService.SendEmailAsync(studentEmail, subject, body);
                _logger.LogWarning($"🚨 Kritik uyarı gönderildi: {studentEmail} - {courseName} ({absenceRate:F1}%)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email gönderilemedi: {studentEmail} - {ex.Message}");
            }
        }

        private string GetCurrentSemester()
        {
            var month = DateTime.Now.Month;
            return month >= 9 || month <= 1 ? "Fall" : "Spring";
        }
    }
}
