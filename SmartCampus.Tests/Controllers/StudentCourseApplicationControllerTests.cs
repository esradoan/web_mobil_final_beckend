using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class StudentCourseApplicationControllerTests
    {
        private readonly Mock<IStudentCourseApplicationService> _mockService;
        private readonly StudentCourseApplicationController _controller;

        public StudentCourseApplicationControllerTests()
        {
            _mockService = new Mock<IStudentCourseApplicationService>();
            _controller = new StudentCourseApplicationController(_mockService.Object);
            SetupUserContext(_controller, userId: 1001, role: "Student");
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
        public async Task CreateApplication_ShouldReturnCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateStudentCourseApplicationDto { CourseId = 1, SectionId = 1 };
            var expected = new StudentCourseApplicationDto { Id = 1, StudentId = 1001, CourseId = 1 };
            _mockService.Setup(s => s.CreateApplicationAsync(1, 1, 1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateApplication(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, createdResult.Value);
        }

        [Fact]
        public async Task CreateApplication_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var dto = new CreateStudentCourseApplicationDto { CourseId = 1, SectionId = 1 };
            _mockService.Setup(s => s.CreateApplicationAsync(1, 1, 1001))
                .ThrowsAsync(new Exception("Already applied"));

            // Act
            var result = await _controller.CreateApplication(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetApplications_AsStudent_ShouldUseStudentIdFromToken()
        {
            // Arrange
            var expected = new StudentApplicationListResponseDto
            {
                Data = new List<StudentCourseApplicationDto>(),
                Total = 0,
                Page = 1,
                PageSize = 10
            };
            _mockService.Setup(s => s.GetApplicationsAsync(1001, null, 1, 10)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetApplications(null, null, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetApplications_AsAdmin_ShouldAllowStudentIdParameter()
        {
            // Arrange
            SetupUserContext(_controller, userId: 2001, role: "Admin");
            var expected = new StudentApplicationListResponseDto
            {
                Data = new List<StudentCourseApplicationDto>(),
                Total = 0,
                Page = 1,
                PageSize = 10
            };
            _mockService.Setup(s => s.GetApplicationsAsync(1001, null, 1, 10)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetApplications(null, 1001, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetApplication_ShouldReturnOk_WhenApplicationExists()
        {
            // Arrange
            var expected = new StudentCourseApplicationDto { Id = 1, StudentId = 1001 };
            _mockService.Setup(s => s.GetApplicationByIdAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetApplication(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetApplication_ShouldReturnNotFound_WhenApplicationDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetApplicationByIdAsync(999)).ReturnsAsync((StudentCourseApplicationDto?)null);

            // Act
            var result = await _controller.GetApplication(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetApplication_AsStudent_ShouldReturnForbid_WhenNotOwnApplication()
        {
            // Arrange
            var expected = new StudentCourseApplicationDto { Id = 1, StudentId = 2001 }; // Different student
            _mockService.Setup(s => s.GetApplicationByIdAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetApplication(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ApproveApplication_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 2001, role: "Admin");
            var expected = new StudentCourseApplicationDto { Id = 1, Status = ApplicationStatus.Approved };
            _mockService.Setup(s => s.ApproveApplicationAsync(1, 2001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.ApproveApplication(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task ApproveApplication_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            SetupUserContext(_controller, userId: 2001, role: "Admin");
            _mockService.Setup(s => s.ApproveApplicationAsync(1, 2001))
                .ThrowsAsync(new Exception("Already processed"));

            // Act
            var result = await _controller.ApproveApplication(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RejectApplication_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 2001, role: "Admin");
            var dto = new RejectStudentApplicationDto { Reason = "Capacity full" };
            var expected = new StudentCourseApplicationDto { Id = 1, Status = ApplicationStatus.Rejected };
            _mockService.Setup(s => s.RejectApplicationAsync(1, 2001, "Capacity full")).ReturnsAsync(expected);

            // Act
            var result = await _controller.RejectApplication(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task RejectApplication_ShouldReturnOk_WithoutReason()
        {
            // Arrange
            SetupUserContext(_controller, userId: 2001, role: "Admin");
            var expected = new StudentCourseApplicationDto { Id = 1, Status = ApplicationStatus.Rejected };
            _mockService.Setup(s => s.RejectApplicationAsync(1, 2001, null)).ReturnsAsync(expected);

            // Act
            var result = await _controller.RejectApplication(1, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task CanApply_ShouldReturnOk_WhenCanApply()
        {
            // Arrange
            _mockService.Setup(s => s.CanStudentApplyAsync(1001, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.CanApply(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CanApply_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            _mockService.Setup(s => s.CanStudentApplyAsync(1001, 1))
                .ThrowsAsync(new Exception("Section not found"));

            // Act
            var result = await _controller.CanApply(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailableCourses_ShouldReturnOk_WithCourses()
        {
            // Arrange
            var expected = new List<CourseDto> { new CourseDto { Id = 1 } };
            _mockService.Setup(s => s.GetAvailableCoursesForStudentAsync(1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAvailableCourses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetAvailableCourses_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            _mockService.Setup(s => s.GetAvailableCoursesForStudentAsync(1001))
                .ThrowsAsync(new Exception("Student not found"));

            // Act
            var result = await _controller.GetAvailableCourses();

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
