using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartCampus.Business.Services
{
    public class SMTPEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SMTPEmailService> _logger;

        public SMTPEmailService(IConfiguration configuration, ILogger<SMTPEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var smtpHost = smtpSettings["Host"];
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"];
                var smtpPassword = smtpSettings["Password"];
                var smtpFromEmail = smtpSettings["FromEmail"];
                var smtpFromName = smtpSettings["FromName"] ?? "Smart Campus";
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("SMTP ayarları eksik! Lütfen appsettings.json'da SmtpSettings bölümünü kontrol edin.");
                    throw new InvalidOperationException("SMTP ayarları yapılandırılmamış. Lütfen appsettings.json'da SmtpSettings bölümünü doldurun.");
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(smtpFromName, smtpFromEmail ?? smtpUsername));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                // Create HTML body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send email using SMTP
                using (var client = new SmtpClient())
                {
                    // Set timeout (30 seconds)
                    client.Timeout = 30000;
                    
                    _logger.LogInformation($"📧 SMTP bağlantısı kuruluyor: {smtpHost}:{smtpPort}");
                    
                    // Connect to SMTP server
                    await client.ConnectAsync(smtpHost, smtpPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    
                    _logger.LogInformation($"✅ SMTP bağlantısı başarılı. Kimlik doğrulama yapılıyor...");
                    
                    // Authenticate
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    
                    _logger.LogInformation($"✅ Kimlik doğrulama başarılı. Email gönderiliyor...");
                    
                    // Send email
                    await client.SendAsync(message);
                    
                    _logger.LogInformation($"✅ Email SMTP sunucusuna gönderildi");
                    
                    // Disconnect
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"✅ Email başarıyla gönderildi: {to} - {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Email gönderme hatası: {to} - {subject}");
                throw;
            }
        }
    }
}

