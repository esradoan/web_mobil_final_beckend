using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.API.Hubs;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class NotificationHubTests
    {
        private readonly Mock<ILogger<NotificationHub>> _mockLogger;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly NotificationHub _hub;

        public NotificationHubTests()
        {
            _mockLogger = new Mock<ILogger<NotificationHub>>();
            _mockGroups = new Mock<IGroupManager>();

            _hub = new NotificationHub(_mockLogger.Object)
            {
                Groups = _mockGroups.Object,
                Context = CreateMockHubCallerContext("connection-123", "1001")
            };
        }

        private HubCallerContext CreateMockHubCallerContext(string connectionId, string userId)
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }))
            };

            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            mockContext.Setup(c => c.User).Returns(httpContext.User);
            return mockContext.Object;
        }

        [Fact]
        public async Task OnConnectedAsync_ShouldAddUserToPersonalGroup()
        {
            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(g => g.AddToGroupAsync("connection-123", "User_1001", default), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_ShouldLogConnection()
        {
            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1001")),
                    It.IsAny<System.Exception>(),
                    It.Is<Func<It.IsAnyType, System.Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldRemoveUserFromGroup()
        {
            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockGroups.Verify(g => g.RemoveFromGroupAsync("connection-123", "User_1001", default), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldLogDisconnection()
        {
            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1001") && v.ToString()!.Contains("disconnected")),
                    It.IsAny<System.Exception>(),
                    It.Is<Func<It.IsAnyType, System.Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
