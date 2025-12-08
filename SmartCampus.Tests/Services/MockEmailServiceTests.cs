#nullable disable
using Xunit;
using SmartCampus.Business.Services;
using System.Threading.Tasks;

namespace SmartCampus.Tests.Services
{
    public class MockEmailServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_DoesNotThrow()
        {
            // Arrange
            var service = new MockEmailService();

            // Act & Assert (should complete without exception)
            await service.SendEmailAsync("test@example.com", "Subject", "Body");
        }

        [Fact]
        public async Task SendEmailAsync_WithHtmlBody_DoesNotThrow()
        {
            // Arrange
            var service = new MockEmailService();
            var htmlBody = "<html><body><h1>Hello</h1></body></html>";

            // Act & Assert
            await service.SendEmailAsync("test@example.com", "HTML Test", htmlBody);
        }
    }
}
