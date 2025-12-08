using System;
using System.Threading.Tasks;

namespace SmartCampus.Business.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            // Simulate sending HTML email
            Console.WriteLine($"[MockEmailService] To: {to}");
            Console.WriteLine($"[MockEmailService] Subject: {subject}");
            Console.WriteLine($"[MockEmailService] Body (HTML Preview): {body.Substring(0, Math.Min(body.Length, 100))}..."); 
            return Task.CompletedTask;
        }
    }
}
