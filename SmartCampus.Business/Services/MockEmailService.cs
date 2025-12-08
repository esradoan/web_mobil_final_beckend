using System;
using System.Threading.Tasks;

namespace SmartCampus.Business.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            // For now, just log to console or debug output
            Console.WriteLine($"[MockEmailService] To: {to}, Subject: {subject}, Body: {body}");
            return Task.CompletedTask;
        }
    }
}
