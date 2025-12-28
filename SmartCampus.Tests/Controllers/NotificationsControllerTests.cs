using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace SmartCampus.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly NotificationsController _controller;

        public NotificationsControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _controller = new NotificationsController(_mockNotificationService.Object);
        }

        private void SetupUserContext(int userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetNotifications_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockNotificationService.Setup(s => s.GetUserNotificationsAsync(1, 1, 20))
                .ReturnsAsync(new List<Notification>());

            // Act
            var result = await _controller.GetNotifications(1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetUnreadCount_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1))
                .ReturnsAsync(5);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task MarkAsRead_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockNotificationService.Setup(s => s.MarkAsReadAsync(100, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAsRead(100);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task MarkAllAsRead_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockNotificationService.Setup(s => s.MarkAllAsReadAsync(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteNotification_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockNotificationService.Setup(s => s.DeleteNotificationAsync(100, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteNotification(100);

            // Assert
            Assert.IsType<OkResult>(result);
        }
    }
}
