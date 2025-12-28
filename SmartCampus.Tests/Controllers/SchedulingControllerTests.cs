using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Text;

namespace SmartCampus.Tests.Controllers
{
    public class SchedulingControllerTests
    {
        private readonly Mock<ISchedulingService> _mockSchedulingService;
        private readonly Mock<IGeneticSchedulingService> _mockGeneticService;
        private readonly SchedulingController _controller;

        public SchedulingControllerTests()
        {
            _mockSchedulingService = new Mock<ISchedulingService>();
            _mockGeneticService = new Mock<IGeneticSchedulingService>();
            _controller = new SchedulingController(_mockSchedulingService.Object, _mockGeneticService.Object);
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
        public async Task GenerateSchedule_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            var dto = new GenerateScheduleDto();
            _mockSchedulingService.Setup(s => s.GenerateScheduleAsync(dto))
                .ReturnsAsync(new ScheduleGenerationResultDto { Success = true });

            // Act
            var result = await _controller.GenerateSchedule(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GenerateSchedule_Failure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            var dto = new GenerateScheduleDto();
            _mockSchedulingService.Setup(s => s.GenerateScheduleAsync(dto))
                .ReturnsAsync(new ScheduleGenerationResultDto { Success = false, Message = "Error" });

            // Act
            var result = await _controller.GenerateSchedule(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithm_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            var dto = new GeneticScheduleRequestDto();
            _mockGeneticService.Setup(s => s.GenerateWithGeneticAlgorithmAsync(dto))
                .ReturnsAsync(new GeneticScheduleResultDto { Success = true });

            // Act
            var result = await _controller.GenerateWithGeneticAlgorithm(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithm_Failure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            var dto = new GeneticScheduleRequestDto();
            _mockGeneticService.Setup(s => s.GenerateWithGeneticAlgorithmAsync(dto))
                .ReturnsAsync(new GeneticScheduleResultDto { Success = false, Message = "Error" });

            // Act
            var result = await _controller.GenerateWithGeneticAlgorithm(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetSchedule_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockSchedulingService.Setup(s => s.GetScheduleAsync("fall", 2024))
                .ReturnsAsync(new List<ScheduleDto>());

            // Act
            var result = await _controller.GetSchedule("fall", 2024);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetScheduleById_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockSchedulingService.Setup(s => s.GetScheduleByIdAsync(1))
                .ReturnsAsync(new ScheduleDto { Id = 1 });

            // Act
            var result = await _controller.GetScheduleById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetScheduleById_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(1);
            _mockSchedulingService.Setup(s => s.GetScheduleByIdAsync(999))
                .ReturnsAsync((ScheduleDto?)null);

            // Act
            var result = await _controller.GetScheduleById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMySchedule_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockSchedulingService.Setup(s => s.GetMyScheduleAsync(1, "fall", 2024))
                .ReturnsAsync(new List<ScheduleDto>());

            // Act
            var result = await _controller.GetMySchedule("fall", 2024);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExportToICal_Success_ReturnsFile()
        {
            // Arrange
            SetupUserContext(1);
            string icalContent = "BEGIN:VCALENDAR\nEND:VCALENDAR";
            _mockSchedulingService.Setup(s => s.ExportToICalAsync(1, "fall", 2024))
                .ReturnsAsync(icalContent);

            // Act
            var result = await _controller.ExportToICal("fall", 2024);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/calendar", fileResult.ContentType);
            Assert.Equal(Encoding.UTF8.GetBytes(icalContent), fileResult.FileContents);
        }
    }
}
