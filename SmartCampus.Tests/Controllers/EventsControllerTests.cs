using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class EventsControllerTests
    {
        private readonly Mock<IEventService> _mockService;
        private readonly EventsController _controller;

        public EventsControllerTests()
        {
            _mockService = new Mock<IEventService>();
            _controller = new EventsController(_mockService.Object);
            SetupUserContext(_controller, userId: 1001);
        }

        private void SetupUserContext(ControllerBase controller, int userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetEvents_ShouldReturnOk_WithEvents()
        {
            // Arrange
            var expected = new List<EventDto> { new EventDto { Id = 1, Title = "Test Event" } };
            _mockService.Setup(s => s.GetEventsAsync(null, null)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetEvents(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetEvent_ShouldReturnOk_WhenEventExists()
        {
            // Arrange
            var expected = new EventDto { Id = 1, Title = "Test Event" };
            _mockService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetEvent(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetEventByIdAsync(999)).ReturnsAsync((EventDto?)null);

            // Act
            var result = await _controller.GetEvent(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateEvent_ShouldReturnCreated_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new CreateEventDto { Title = "New Event" };
            var expected = new EventDto { Id = 1, Title = "New Event" };
            _mockService.Setup(s => s.CreateEventAsync(1001, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateEvent(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, createdResult.Value);
        }

        [Fact]
        public async Task UpdateEvent_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UpdateEventDto { Title = "Updated Event" };
            var expected = new EventDto { Id = 1, Title = "Updated Event" };
            _mockService.Setup(s => s.UpdateEventAsync(1, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.UpdateEvent(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task DeleteEvent_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            _mockService.Setup(s => s.DeleteEventAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteEvent(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var expected = new EventRegistrationDto { Id = 1, EventId = 1, UserId = 1001 };
            _mockService.Setup(s => s.RegisterAsync(1001, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.Register(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task CancelRegistration_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.CancelRegistrationAsync(1001, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.CancelRegistration(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
       public async Task GetMyEvents_ShouldReturnOk_WithEvents()
        {
            // Arrange
            var expected = new List<EventRegistrationDto> 
            { 
                new EventRegistrationDto { Id = 1, EventId = 1, UserId = 1001 } 
            };
            _mockService.Setup(s => s.GetMyRegistrationsAsync(1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetMyEvents();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CheckIn_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new CheckInDto { QrCode = "ABC123" };
            var expected = new EventRegistrationDto { Id = 1, EventId = 1, CheckedIn = true };
            _mockService.Setup(s => s.CheckInAsync(1, "ABC123")).ReturnsAsync(expected);

            // Act
            var result = await _controller.CheckIn(1, dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
