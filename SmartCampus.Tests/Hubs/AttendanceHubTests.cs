using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SmartCampus.API.Hubs;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class AttendanceHubTests
    {
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly AttendanceHub _hub;

        public AttendanceHubTests()
        {
            _mockGroups = new Mock<IGroupManager>();
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

            _hub = new AttendanceHub
            {
                Groups = _mockGroups.Object,
                Clients = _mockClients.Object,
                Context = CreateMockHubCallerContext("test-connection-id", "1001")
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
        public async Task JoinSession_ShouldAddToGroup()
        {
            // Act
            await _hub.JoinSession(100);

            // Assert
            _mockGroups.Verify(g => g.AddToGroupAsync("test-connection-id", "session_100", default), Times.Once);
        }

        [Fact]
        public async Task JoinSession_ShouldSendConfirmation()
        {
            // Act
            await _hub.JoinSession(100);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "JoinedSession",
                    It.Is<object[]>(o => o.Length == 1),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task LeaveSession_ShouldRemoveFromGroup()
        {
            // Act
            await _hub.LeaveSession(100);

            // Assert
            _mockGroups.Verify(g => g.RemoveFromGroupAsync("test-connection-id", "session_100", default), Times.Once);
        }

        [Fact]
        public async Task LeaveSession_ShouldSendConfirmation()
        {
            // Act
            await _hub.LeaveSession(100);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "LeftSession",
                    It.Is<object[]>(o => o.Length == 1),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_ShouldSendWelcomeMessage()
        {
            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "Connected",
                    It.Is<object[]>(o => o.Length == 1),
                    default),
                Times.Once);
        }
    }
}
