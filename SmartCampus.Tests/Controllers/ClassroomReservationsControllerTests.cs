#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services; 
// Using SmartCampus.Business.Services handles DTOs defined in the service file.
// Removed SmartCampus.Business.DTOs to avoid ambiguity if any.
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class ClassroomReservationsControllerTests
    {
        private readonly Mock<IClassroomReservationService> _mockService;
        private readonly ClassroomReservationsController _controller;

        public ClassroomReservationsControllerTests()
        {
            _mockService = new Mock<IClassroomReservationService>();
            _controller = new ClassroomReservationsController(_mockService.Object);
        }

        private void SetupHttpContext(string userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateReservation_ReturnsCreatedAtAction_WhenSuccessful()
        {
            SetupHttpContext("100");
            var dto = new SmartCampus.Business.Services.CreateClassroomReservationDto { ClassroomId = 1 };
            var resultDto = new SmartCampus.Business.Services.ClassroomReservationDto { Id = 5 };

            _mockService.Setup(x => x.CreateReservationAsync(100, dto)).ReturnsAsync(resultDto);

            var result = await _controller.CreateReservation(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(resultDto, createdResult.Value);
        }

        [Fact]
        public async Task GetReservations_ReturnsOk()
        {
            SetupHttpContext("100");
            var list = new List<SmartCampus.Business.Services.ClassroomReservationDto>();
            _mockService.Setup(x => x.GetReservationsAsync(null, null, null)).ReturnsAsync(list);

            var result = await _controller.GetReservations(null, null, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Controller returns { data = list }
            dynamic val = okResult.Value;
            Assert.Same(list, val.GetType().GetProperty("data").GetValue(val));
        }

        [Fact]
        public async Task GetReservation_ReturnsOk_WhenExists()
        {
            SetupHttpContext("100");
            var dto = new SmartCampus.Business.Services.ClassroomReservationDto { Id = 5 };
            _mockService.Setup(x => x.GetReservationByIdAsync(5)).ReturnsAsync(dto);

            var result = await _controller.GetReservation(5);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task GetReservation_ReturnsNotFound_WhenNotExists()
        {
            SetupHttpContext("100");
            _mockService.Setup(x => x.GetReservationByIdAsync(99)).ReturnsAsync((SmartCampus.Business.Services.ClassroomReservationDto)null);

            var result = await _controller.GetReservation(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelReservation_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("100");
            _mockService.Setup(x => x.CancelReservationAsync(100, 5)).ReturnsAsync(true);

            var result = await _controller.CancelReservation(5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CancelReservation_ReturnsNotFound_WhenFailed()
        {
            SetupHttpContext("100");
            _mockService.Setup(x => x.CancelReservationAsync(100, 5)).ReturnsAsync(false);

            var result = await _controller.CancelReservation(5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetPendingReservations_ReturnsOk()
        {
            SetupHttpContext("1", "Admin");
            var list = new List<SmartCampus.Business.Services.ClassroomReservationDto>();
            _mockService.Setup(x => x.GetPendingReservationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetPendingReservations();

            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic val = okResult.Value;
            Assert.Same(list, val.GetType().GetProperty("data").GetValue(val));
        }

        [Fact]
        public async Task ApproveReservation_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Admin");
            // ApprovalDto is in API.Controllers namespace
            var dto = new SmartCampus.API.Controllers.ApprovalDto { Notes = "Ok" };
            var resultDto = new SmartCampus.Business.Services.ClassroomReservationDto { Id = 5 };
            
            _mockService.Setup(x => x.ApproveReservationAsync(1, 5, "Ok")).ReturnsAsync(resultDto);

            var result = await _controller.ApproveReservation(5, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RejectReservation_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Admin");
            var dto = new SmartCampus.API.Controllers.ApprovalDto { Notes = "No" };
            var resultDto = new SmartCampus.Business.Services.ClassroomReservationDto { Id = 5 };

            _mockService.Setup(x => x.RejectReservationAsync(1, 5, "No")).ReturnsAsync(resultDto);

            var result = await _controller.RejectReservation(5, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClassroomAvailability_ReturnsOk()
        {
            var list = new List<SmartCampus.Business.Services.ClassroomAvailabilityDto>();
            _mockService.Setup(x => x.GetClassroomAvailabilityAsync(1, It.IsAny<DateTime>())).ReturnsAsync(list);

            var result = await _controller.GetClassroomAvailability(1, DateTime.Now);

            var okResult = Assert.IsType<OkObjectResult>(result);
        }
        
        [Fact]
        public async Task GetAvailableClassrooms_ReturnsOk()
        {
            // Correction: Use AvailableClassroomDto
            var list = new List<SmartCampus.Business.Services.AvailableClassroomDto>();
            _mockService.Setup(x => x.GetAvailableClassroomsAsync(It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>())).ReturnsAsync(list);

            var result = await _controller.GetAvailableClassrooms(DateTime.Now, TimeSpan.Zero, TimeSpan.Zero);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}
