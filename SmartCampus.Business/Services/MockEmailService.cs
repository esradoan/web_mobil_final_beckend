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
📧 MOCK EMAIL SENT
========================================
To: {to}
Subject: {subject}
Time: {DateTime.Now}
----------------------------------------
{body}
========================================
";
            // Write to console
            Console.WriteLine(emailLog);

            // Also write to a file for easy access
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mock_emails.txt");
            File.AppendAllText(filePath, emailLog);

            Console.WriteLine($"📁 Email also saved to: {filePath}");

            return Task.CompletedTask;
        }
    }
}
