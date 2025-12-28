using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class AnalyticsControllerTests
    {
        private readonly Mock<IAttendanceAnalyticsService> _mockService;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            _mockService = new Mock<IAttendanceAnalyticsService>();
            _controller = new AnalyticsController(_mockService.Object);
        }

        private void SetupUserContext(int userId, string role = "Faculty")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetAttendanceTrends_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.GetAttendanceTrendsAsync(1))
                .ReturnsAsync(new AttendanceTrendDto { SectionId = 1 });

            // Act
            var result = await _controller.GetAttendanceTrends(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAttendanceTrends_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.GetAttendanceTrendsAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            // Act
            var result = await _controller.GetAttendanceTrends(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetStudentRiskAnalysis_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.GetStudentRiskAnalysisAsync(2))
                .ReturnsAsync(new StudentRiskAnalysisDto { StudentId = 2 });

            // Act
            var result = await _controller.GetStudentRiskAnalysis(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetStudentRiskAnalysis_StudentViewingOtherStudent_ReturnsForbid()
        {
            // Arrange
            SetupUserContext(1, "Student");

            // Act - Student 1 trying to view Student 2's risk
            var result = await _controller.GetStudentRiskAnalysis(2);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetStudentRiskAnalysis_StudentViewingOwn_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Student");
            _mockService.Setup(s => s.GetStudentRiskAnalysisAsync(1))
                .ReturnsAsync(new StudentRiskAnalysisDto { StudentId = 1 });

            // Act
            var result = await _controller.GetStudentRiskAnalysis(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMyRiskAnalysis_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Student");
            _mockService.Setup(s => s.GetStudentRiskAnalysisAsync(1))
                .ReturnsAsync(new StudentRiskAnalysisDto { StudentId = 1 });

            // Act
            var result = await _controller.GetMyRiskAnalysis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMyRiskAnalysis_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(999, "Student");
            _mockService.Setup(s => s.GetStudentRiskAnalysisAsync(999))
                .ThrowsAsync(new Exception("Student not found"));

            // Act
            var result = await _controller.GetMyRiskAnalysis();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSectionAnalytics_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.GetSectionAnalyticsAsync(1))
                .ReturnsAsync(new SectionAnalyticsDto { SectionId = 1 });

            // Act
            var result = await _controller.GetSectionAnalytics(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSectionAnalytics_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.GetSectionAnalyticsAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            // Act
            var result = await _controller.GetSectionAnalytics(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCampusAnalytics_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            _mockService.Setup(s => s.GetCampusAnalyticsAsync())
                .ReturnsAsync(new CampusAnalyticsDto { TotalStudents = 100 });

            // Act
            var result = await _controller.GetCampusAnalytics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExportSectionPdf_Success_ReturnsFile()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.ExportSectionReportAsync(1))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.ExportSectionPdf(1);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        [Fact]
        public async Task ExportSectionPdf_Error_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.ExportSectionReportAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            // Act
            var result = await _controller.ExportSectionPdf(999);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ExportSectionExcel_Success_ReturnsFile()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.ExportSectionReportToExcelAsync(1))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.ExportSectionExcel(1);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
        }

        [Fact]
        public async Task ExportSectionExcel_Error_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1, "Faculty");
            _mockService.Setup(s => s.ExportSectionReportToExcelAsync(999))
                .ThrowsAsync(new Exception("Section not found"));

            // Act
            var result = await _controller.ExportSectionExcel(999);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
