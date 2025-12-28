using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace SmartCampus.Tests.Controllers
{
    public class GradesControllerTests
    {
        private readonly Mock<IEnrollmentService> _mockService;
        private readonly Mock<ITranscriptPdfService> _mockPdfService;
        private readonly GradesController _controller;

        public GradesControllerTests()
        {
            _mockService = new Mock<IEnrollmentService>();
            _mockPdfService = new Mock<ITranscriptPdfService>();
            _controller = new GradesController(_mockService.Object, _mockPdfService.Object);
        }

        private void SetupUserContext(int userId, string role = "Student")
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
        public async Task GetMyGrades_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockService.Setup(s => s.GetMyGradesAsync(1))
                .ReturnsAsync(new MyGradesDto());

            // Act
            var result = await _controller.GetMyGrades();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetTranscript_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockService.Setup(s => s.GetTranscriptAsync(1))
                .ReturnsAsync(new TranscriptDto());

            // Act
            var result = await _controller.GetTranscript();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetTranscript_Exception_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(1);
            _mockService.Setup(s => s.GetTranscriptAsync(1))
                .ThrowsAsync(new Exception("Not found"));

            // Act
            var result = await _controller.GetTranscript();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetTranscriptPdf_Success_ReturnsFile()
        {
            // Arrange
            SetupUserContext(1);
            _mockService.Setup(s => s.GetTranscriptAsync(1))
                .ReturnsAsync(new TranscriptDto { StudentNumber = "123" });
            _mockPdfService.Setup(s => s.GenerateTranscript(It.IsAny<TranscriptDto>()))
                .Returns(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.GetTranscriptPdf();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("transcript_123.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetTranscriptPdf_Exception_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(1);
            _mockService.Setup(s => s.GetTranscriptAsync(1))
                .ThrowsAsync(new Exception("Not found"));

            // Act
            var result = await _controller.GetTranscriptPdf();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task EnterGrade_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(2, "Faculty");
            var dto = new GradeInputDto();
            _mockService.Setup(s => s.EnterGradeAsync(2, dto))
                .ReturnsAsync(new GradeResultDto());

            // Act
            var result = await _controller.EnterGrade(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task EnterGrade_Unauthorized_ReturnsForbid()
        {
            // Arrange
            SetupUserContext(2, "Faculty");
            var dto = new GradeInputDto();
            _mockService.Setup(s => s.EnterGradeAsync(2, dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

            // Act
            var result = await _controller.EnterGrade(dto);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task EnterGrade_Exception_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(2, "Faculty");
            var dto = new GradeInputDto();
            _mockService.Setup(s => s.EnterGradeAsync(2, dto))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.EnterGrade(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
