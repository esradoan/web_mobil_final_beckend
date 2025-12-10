using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartCampus.Business.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            var emailLog = $@"
========================================
📧 MOCK EMAIL SENT (DEVELOPMENT MODE)
========================================
⚠️  NOT: This is a MOCK email service. No actual email is sent!
    In production, configure a real email service (SMTP, SendGrid, etc.)

To: {to}
Subject: {subject}
Time: {DateTime.Now}
----------------------------------------
{body}
========================================
";
            // Write to console with prominent warning
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("⚠️  MOCK EMAIL SERVICE - NO ACTUAL EMAIL SENT!");
            Console.WriteLine(new string('=', 60));
            Console.ResetColor();
            Console.WriteLine(emailLog);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("💡 TIP: Check console output or mock_emails.txt file for email content");
            Console.WriteLine("💡 TIP: In production, replace MockEmailService with real SMTP service");
            Console.ResetColor();

            // Also write to a file for easy access
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mock_emails.txt");
            File.AppendAllText(filePath, emailLog);

            Console.WriteLine($"📁 Email also saved to: {filePath}");

            return Task.CompletedTask;
        }
    }
}
