#nullable disable
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
    public class CourseApplicationControllerTests
    {
        private readonly Mock<ICourseApplicationService> _mockService;
        private readonly CourseApplicationController _controller;

        public CourseApplicationControllerTests()
        {
            _mockService = new Mock<ICourseApplicationService>();
            _controller = new CourseApplicationController(_mockService.Object);
        }

        private void SetupHttpContext(string userId, string role = "Admin")
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
        public async Task CreateApplication_ReturnsCreatedAtAction_WhenSuccessful()
        {
            SetupHttpContext("10", "Faculty");
            var dto = new CreateCourseApplicationDto { CourseId = 100 };
            var appDto = new CourseApplicationDto { Id = 1, InstructorId = 10, CourseId = 100 };
            
            _mockService.Setup(x => x.CreateApplicationAsync(dto.CourseId, 10)).ReturnsAsync(appDto);

            var result = await _controller.CreateApplication(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(appDto, createdResult.Value);
        }

        [Fact]
        public async Task GetApplications_ReturnsOk_AdminSeesAll()
        {
            SetupHttpContext("1", "Admin");
            var resultDto = new ApplicationListResponseDto { Data = new List<CourseApplicationDto>(), Total = 0 };
            // Admin can see any instructor's applications or all
            _mockService.Setup(x => x.GetApplicationsAsync(null, null, 1, 10)).ReturnsAsync(resultDto);

            var result = await _controller.GetApplications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task GetApplications_ReturnsOk_FacultySeesOwn()
        {
            SetupHttpContext("5", "Faculty");
            var resultDto = new ApplicationListResponseDto { Data = new List<CourseApplicationDto>(), Total = 0 };
            // Faculty ID is enforced
            _mockService.Setup(x => x.GetApplicationsAsync(5, null, 1, 10)).ReturnsAsync(resultDto);

            var result = await _controller.GetApplications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task GetApplication_ReturnsOk_WhenAllowed()
        {
            SetupHttpContext("5", "Faculty");
            var appDto = new CourseApplicationDto { Id = 1, InstructorId = 5 }; // Own application
            _mockService.Setup(x => x.GetApplicationByIdAsync(1)).ReturnsAsync(appDto);

            var result = await _controller.GetApplication(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(appDto, okResult.Value);
        }

        [Fact]
        public async Task GetApplication_ReturnsForbid_WhenFacultyAccessOthers()
        {
            SetupHttpContext("6", "Faculty");
            var appDto = new CourseApplicationDto { Id = 1, InstructorId = 5 }; // Other's application
            _mockService.Setup(x => x.GetApplicationByIdAsync(1)).ReturnsAsync(appDto);

            var result = await _controller.GetApplication(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetApplication_ReturnsNotFound_WhenNotExists()
        {
            SetupHttpContext("1", "Admin");
            _mockService.Setup(x => x.GetApplicationByIdAsync(99)).ReturnsAsync((CourseApplicationDto)null);

            var result = await _controller.GetApplication(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ApproveApplication_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Admin");
            var appDto = new CourseApplicationDto { Id = 1, Status = ApplicationStatus.Approved };
            _mockService.Setup(x => x.ApproveApplicationAsync(1, 1)).ReturnsAsync(appDto);

            var result = await _controller.ApproveApplication(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(appDto, okResult.Value);
        }

        [Fact]
        public async Task RejectApplication_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Admin");
            var dto = new RejectApplicationDto { Reason = "Reason" };
            var appDto = new CourseApplicationDto { Id = 1, Status = ApplicationStatus.Rejected };
            _mockService.Setup(x => x.RejectApplicationAsync(1, 1, dto.Reason)).ReturnsAsync(appDto);

            var result = await _controller.RejectApplication(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(appDto, okResult.Value);
        }

        [Fact]
        public async Task CanApply_ReturnsOk_WithResult()
        {
            SetupHttpContext("10", "Faculty");
            _mockService.Setup(x => x.CanInstructorApplyAsync(10, 100)).ReturnsAsync(true);

            var result = await _controller.CanApply(100);

            var okResult = Assert.IsType<OkObjectResult>(result);
            // Verify structure { canApply = true } via dynamic/reflection
            dynamic val = okResult.Value;
            Assert.True(val.GetType().GetProperty("canApply").GetValue(val));
        }
    }
}
