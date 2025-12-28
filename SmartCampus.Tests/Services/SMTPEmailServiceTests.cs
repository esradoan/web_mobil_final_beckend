using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.Business.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class SMTPEmailServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<SMTPEmailService>> _mockLogger;
        private readonly Mock<IConfigurationSection> _mockSmtpSection;

        public SMTPEmailServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<SMTPEmailService>>();
            _mockSmtpSection = new Mock<IConfigurationSection>();
        }

        [Fact]
        public async Task SendEmailAsync_WithMissingConfig_ShouldThrowException()
        {
            // Arrange
            _mockConfig.Setup(c => c.GetSection("SmtpSettings")).Returns(_mockSmtpSection.Object);
            _mockSmtpSection.Setup(s => s["Host"]).Returns((string)null);
            
            var service = new SMTPEmailService(_mockConfig.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendEmailAsync("test@test.com", "Subject", "Body"));
        }

        [Fact]
        public async Task SendEmailAsync_ShouldLogError_WhenConfigMissing()
        {
            // Arrange
            _mockConfig.Setup(c => c.GetSection("SmtpSettings")).Returns(_mockSmtpSection.Object);
            _mockSmtpSection.Setup(s => s["Host"]).Returns((string)null);
            
            var service = new SMTPEmailService(_mockConfig.Object, _mockLogger.Object);

            // Act
            try
            {
                await service.SendEmailAsync("test@test.com", "Test", "Body");
            }
            catch { }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMTP ayarları eksik")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public void SMTPEmailService_Constructor_ShouldNotThrow()
        {
            // Act & Assert
            var service = new SMTPEmailService(_mockConfig.Object, _mockLogger.Object);
            Assert.NotNull(service);
        }
    }
}
