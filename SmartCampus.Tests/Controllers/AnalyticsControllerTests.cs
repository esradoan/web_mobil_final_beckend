#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System;

namespace SmartCampus.Tests.Controllers
{
    public class AnalyticsControllerTests
    {
        private readonly Mock<IAttendanceAnalyticsService> _mockAnalyticsService;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            _mockAnalyticsService = new Mock<IAttendanceAnalyticsService>();
            _controller = new AnalyticsController(_mockAnalyticsService.Object);
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

        // GetAttendanceTrends Tests
        [Fact]
        public async Task GetAttendanceTrends_ReturnsOk_WhenTrendsExist()
        {
            SetupHttpContext("1", "Faculty");
            var trends = new AttendanceTrendDto { SectionId = 1 };
            _mockAnalyticsService.Setup(x => x.GetAttendanceTrendsAsync(1)).ReturnsAsync(trends);

            var result = await _controller.GetAttendanceTrends(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAttendanceTrends_ReturnsNotFound_WhenSectionNotExists()
        {
            SetupHttpContext("1", "Faculty");
            _mockAnalyticsService.Setup(x => x.GetAttendanceTrendsAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            var result = await _controller.GetAttendanceTrends(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // GetStudentRiskAnalysis Tests
        [Fact]
        public async Task GetStudentRiskAnalysis_ReturnsOk_WhenStudentExists()
        {
            SetupHttpContext("1", "Faculty");
            var risk = new StudentRiskAnalysisDto { StudentId = 10, RiskLevel = "Low" };
            _mockAnalyticsService.Setup(x => x.GetStudentRiskAnalysisAsync(10)).ReturnsAsync(risk);

            var result = await _controller.GetStudentRiskAnalysis(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetStudentRiskAnalysis_ReturnsForbid_WhenStudentTriesToViewAnothersRisk()
        {
            SetupHttpContext("1", "Student");
            // Student 1 tries to view Student 10's risk

            var result = await _controller.GetStudentRiskAnalysis(10);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetStudentRiskAnalysis_ReturnsOk_WhenStudentViewsOwnRisk()
        {
            SetupHttpContext("10", "Student");
            var risk = new StudentRiskAnalysisDto { StudentId = 10, RiskLevel = "Low" };
            _mockAnalyticsService.Setup(x => x.GetStudentRiskAnalysisAsync(10)).ReturnsAsync(risk);

            var result = await _controller.GetStudentRiskAnalysis(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // GetMyRiskAnalysis Tests
        [Fact]
        public async Task GetMyRiskAnalysis_ReturnsOk_WhenCalled()
        {
            SetupHttpContext("5", "Student");
            var risk = new StudentRiskAnalysisDto { StudentId = 5, RiskLevel = "Medium" };
            _mockAnalyticsService.Setup(x => x.GetStudentRiskAnalysisAsync(5)).ReturnsAsync(risk);

            var result = await _controller.GetMyRiskAnalysis();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMyRiskAnalysis_ReturnsNotFound_WhenStudentNotExists()
        {
            SetupHttpContext("999", "Student");
            _mockAnalyticsService.Setup(x => x.GetStudentRiskAnalysisAsync(999))
                .ThrowsAsync(new Exception("Student not found"));

            var result = await _controller.GetMyRiskAnalysis();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // GetSectionAnalytics Tests
        [Fact]
        public async Task GetSectionAnalytics_ReturnsOk_WhenSectionExists()
        {
            SetupHttpContext("1", "Faculty");
            var analytics = new SectionAnalyticsDto { SectionId = 1 };
            _mockAnalyticsService.Setup(x => x.GetSectionAnalyticsAsync(1)).ReturnsAsync(analytics);

            var result = await _controller.GetSectionAnalytics(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSectionAnalytics_ReturnsNotFound_WhenSectionNotExists()
        {
            SetupHttpContext("1", "Faculty");
            _mockAnalyticsService.Setup(x => x.GetSectionAnalyticsAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            var result = await _controller.GetSectionAnalytics(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // GetCampusAnalytics Tests
        [Fact]
        public async Task GetCampusAnalytics_ReturnsOk_WhenAdminCalls()
        {
            SetupHttpContext("1", "Admin");
            var analytics = new CampusAnalyticsDto();
            _mockAnalyticsService.Setup(x => x.GetCampusAnalyticsAsync()).ReturnsAsync(analytics);

            var result = await _controller.GetCampusAnalytics();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
