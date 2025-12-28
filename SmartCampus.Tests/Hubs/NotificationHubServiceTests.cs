using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.API.Hubs;
using SmartCampus.API.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class NotificationHubServiceTests
    {
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<ILogger<NotificationHubService>> _mockLogger;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly NotificationHubService _service;

        public NotificationHubServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockLogger = new Mock<ILogger<NotificationHubService>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _service = new NotificationHubService(_mockHubContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_ShouldSendToUserGroup()
        {
            // Arrange
            var notification = new { Title = "Test", Message = "Hello" };

            // Act
            await _service.SendNotificationToUserAsync("123", notification);

            // Assert
            _mockClients.Verify(c => c.Group("User_123"), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveNotification",
                    It.Is<object[]>(o => o.Length == 1 && o[0] == notification),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_WhenException_ShouldLogError()
        {
            // Arrange
            _mockClientProxy.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default))
                .ThrowsAsync(new Exception("Connection lost"));

            // Act
            await _service.SendNotificationToUserAsync("123", new { });

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationToAllAsync_ShouldBroadcastToAll()
        {
            // Arrange
            var notification = new { Title = "Announcement", Message = "System maintenance" };

            // Act
            await _service.SendNotificationToAllAsync(notification);

            // Assert
            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveBroadcast",
                    It.Is<object[]>(o => o.Length == 1 && o[0] == notification),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendNotificationToAllAsync_WhenException_ShouldLogError()
        {
            // Arrange
            _mockClientProxy.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default))
                .ThrowsAsync(new Exception("Broadcast failed"));

            // Act
            await _service.SendNotificationToAllAsync(new { });

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
