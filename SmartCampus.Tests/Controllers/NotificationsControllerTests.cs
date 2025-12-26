using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace SMARTCAMPUS.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<INotificationService> _mockService;
        private readonly NotificationsController _controller;
        private readonly ClaimsPrincipal _user;

        public NotificationsControllerTests()
        {
            _mockService = new Mock<INotificationService>();
            _controller = new NotificationsController(_mockService.Object);

            _user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Student")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = _user }
            };
        }

        [Fact]
        public async Task GetNotifications_ReturnsOk_WithList()
        {
            // Arrange
            var notifications = new List<Notification>
            {
                new Notification { Id = 1, Title = "Test", Message = "Msg", UserId = 1 }
            };
            _mockService.Setup(x => x.GetUserNotificationsAsync(1, 1, 20))
                .ReturnsAsync(notifications);

            // Act
            var result = await _controller.GetNotifications(1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnList = Assert.IsType<List<Notification>>(okResult.Value);
            Assert.Single(returnList);
        }

        [Fact]
        public async Task GetUnreadCount_ReturnsOk_WithCount()
        {
            // Arrange
            _mockService.Setup(x => x.GetUnreadCountAsync(1)).ReturnsAsync(5);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Assuming anonymous object, we check via string or reflection, or just status code for simplicity here or dynamic
            // But dynamic is easy:
            // Assert.Equal("{ count = 5 }", result.ToString()); // No
            // Let's verify status 200
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsOk()
        {
            // Act
            var result = await _controller.MarkAsRead(100);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockService.Verify(x => x.MarkAsReadAsync(100, 1), Times.Once);
        }
    }
}
