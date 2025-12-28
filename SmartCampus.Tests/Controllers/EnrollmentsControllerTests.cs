using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class EnrollmentsControllerTests
    {
        private readonly Mock<IEnrollmentService> _mockService;
        private readonly EnrollmentsController _controller;

        public EnrollmentsControllerTests()
        {
            _mockService = new Mock<IEnrollmentService>();
            _controller = new EnrollmentsController(_mockService.Object);
            SetupUserContext(_controller, userId: 1001);
        }

        private void SetupUserContext(ControllerBase controller, int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Enroll_ShouldReturnCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateEnrollmentDto { SectionId = 1 };
            var expected = new EnrollmentDto { Id = 1, SectionId = 1 };
            _mockService.Setup(s => s.EnrollAsync(1001, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.Enroll(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, createdResult.Value);
        }

        [Fact]
        public async Task Enroll_ShouldReturnBadRequest_WhenPrerequisiteCheckFails()
        {
            // Arrange
            var dto = new CreateEnrollmentDto { SectionId = 1 };
            _mockService.Setup(s => s.EnrollAsync(1001, 1))
                .ThrowsAsync(new InvalidOperationException("Prerequisite not met"));

            // Act
            var result = await _controller.Enroll(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DropCourse_ShouldReturnNoContent_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.DropCourseAsync(1, 1001)).ReturnsAsync(true);

            // Act
            var result = await _controller.DropCourse(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DropCourse_ShouldReturnNotFound_WhenEnrollmentNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DropCourseAsync(1, 1001)).ReturnsAsync(false);

            // Act
            var result = await _controller.DropCourse(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyCourses_ShouldReturnOk_WithCourses()
        {
            // Arrange
            var expected = new List<MyCoursesDto>
            {
                new MyCoursesDto { Id = 1, Status = "enrolled" }
            };
            _mockService.Setup(s => s.GetMyCoursesAsync(1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetMyCourses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSectionStudents_ShouldReturnOk_WithStudents()
        {
            // Arrange
            var expected = new List<StudentEnrollmentDto>();
            _mockService.Setup(s => s.GetSectionStudentsAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetSectionStudents(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }
    }
}
