#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.API.Services;
using SmartCampus.Business.Services;
using SmartCampus.Business.DTOs;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System;

namespace SmartCampus.Tests.Controllers
{
    public class AttendanceControllerTests
    {
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly Mock<IQrCodeService> _mockQrCodeService;
        private readonly Mock<IAttendanceHubService> _mockHubService;
        private readonly AttendanceController _controller;

        public AttendanceControllerTests()
        {
            _mockAttendanceService = new Mock<IAttendanceService>();
            _mockQrCodeService = new Mock<IQrCodeService>();
            _mockHubService = new Mock<IAttendanceHubService>();
            _controller = new AttendanceController(
                _mockAttendanceService.Object,
                _mockQrCodeService.Object,
                _mockHubService.Object);
            
            SetupDefaultHttpContext();
        }

        private void SetupDefaultHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
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
            var httpContext = new DefaultHttpContext { User = user };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        // CreateSession Tests
        [Fact]
        public async Task CreateSession_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var dto = new CreateAttendanceSessionDto { 
                SectionId = 1, 
                Date = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0)
            };
            var session = new AttendanceSessionDto { Id = 1, SectionId = 1 };
            _mockAttendanceService.Setup(x => x.CreateSessionAsync(1, It.IsAny<CreateAttendanceSessionDto>())).ReturnsAsync(session);

            var result = await _controller.CreateSession(dto);

            // Note: Controller uses CreatedAtAction which is 201 Created
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenFails()
        {
            SetupHttpContext("1", "Faculty");
            var dto = new CreateAttendanceSessionDto { SectionId = 999 };
            _mockAttendanceService.Setup(x => x.CreateSessionAsync(1, It.IsAny<CreateAttendanceSessionDto>()))
                .ThrowsAsync(new Exception("Section not found"));

            var result = await _controller.CreateSession(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // GetSession Tests
        [Fact]
        public async Task GetSession_ReturnsOk_WhenSessionExists()
        {
            SetupHttpContext("1", "Faculty");
            var session = new AttendanceSessionDto { Id = 1, SectionId = 1 };
            _mockAttendanceService.Setup(x => x.GetSessionByIdAsync(1)).ReturnsAsync(session);

            var result = await _controller.GetSession(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSession_ReturnsNotFound_WhenSessionNotExists()
        {
            SetupHttpContext("1", "Faculty");
            _mockAttendanceService.Setup(x => x.GetSessionByIdAsync(999)).ReturnsAsync((AttendanceSessionDto)null);

            var result = await _controller.GetSession(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // CloseSession Tests
        [Fact]
        public async Task CloseSession_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            _mockAttendanceService.Setup(x => x.CloseSessionAsync(1, 1)).ReturnsAsync(true);

            var result = await _controller.CloseSession(1);

            Assert.IsType<OkObjectResult>(result);
        }

        // GetMySessions Tests
        [Fact]
        public async Task GetMySessions_ReturnsOk_WhenFacultyCalls()
        {
            SetupHttpContext("1", "Faculty");
            var sessions = new List<AttendanceSessionDto> { new AttendanceSessionDto { Id = 1 } };
            _mockAttendanceService.Setup(x => x.GetMySessionsAsync(1)).ReturnsAsync(sessions);

            var result = await _controller.GetMySessions();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // CheckIn Tests
        [Fact]
        public async Task CheckIn_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Student");
            var dto = new CheckInRequestDto { Latitude = 41.0m, Longitude = 29.0m, Accuracy = 10m };
            var checkInResult = new CheckInResponseDto { Message = "Success" };
            _mockAttendanceService.Setup(x => x.CheckInAsync(1, 1, It.IsAny<CheckInRequestDto>(), It.IsAny<string>()))
                .ReturnsAsync(checkInResult);

            var result = await _controller.CheckIn(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // GetActiveSessions Tests
        [Fact]
        public async Task GetActiveSessions_ReturnsOk_WhenStudentCalls()
        {
            SetupHttpContext("1", "Student");
            var sessions = new List<AttendanceSessionDto> { new AttendanceSessionDto { Id = 1 } };
            _mockAttendanceService.Setup(x => x.GetActiveSessionsForStudentAsync(1)).ReturnsAsync(sessions);

            var result = await _controller.GetActiveSessions();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // GetMyAttendance Tests
        [Fact]
        public async Task GetMyAttendance_ReturnsOk_WhenStudentCalls()
        {
            SetupHttpContext("1", "Student");
            var attendance = new List<MyAttendanceDto>();
            _mockAttendanceService.Setup(x => x.GetMyAttendanceAsync(1)).ReturnsAsync(attendance);

            var result = await _controller.GetMyAttendance();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // CreateExcuseRequest Tests
        [Fact]
        public async Task CreateExcuseRequest_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Student");
            var dto = new CreateExcuseRequestDto { SessionId = 1, Reason = "Medical" };
            var excuseResult = new ExcuseRequestDto { Id = 1 };
            _mockAttendanceService.Setup(x => x.CreateExcuseRequestAsync(1, It.IsAny<CreateExcuseRequestDto>(), It.IsAny<string>()))
                .ReturnsAsync(excuseResult);

            // Added null for IFormFile parameter
            var result = await _controller.CreateExcuseRequest(dto, null);

            // CHANGED: Controller returns CreatedAtAction (201)
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(createdResult.Value);
        }

        // GetExcuseRequests Tests
        [Fact]
        public async Task GetExcuseRequests_ReturnsOk_WhenFacultyCalls()
        {
            SetupHttpContext("1", "Faculty");
            var requests = new List<ExcuseRequestDto>();
            _mockAttendanceService.Setup(x => x.GetExcuseRequestsAsync(1)).ReturnsAsync(requests);

            // Removed ID parameter
            var result = await _controller.GetExcuseRequests();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // ApproveExcuse Tests
        [Fact]
        public async Task ApproveExcuse_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var dto = new ReviewExcuseDto { Notes = "Approved" };
            _mockAttendanceService.Setup(x => x.ApproveExcuseAsync(1, 1, It.IsAny<string>())).ReturnsAsync(true);

            // Added DTO parameter
            var result = await _controller.ApproveExcuse(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // RejectExcuse Tests
        [Fact]
        public async Task RejectExcuse_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var dto = new ReviewExcuseDto { Notes = "Rejected" };
            _mockAttendanceService.Setup(x => x.RejectExcuseAsync(1, 1, It.IsAny<string>())).ReturnsAsync(true);

            // Added DTO parameter
            var result = await _controller.RejectExcuse(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // GetAttendanceReport Tests
        [Fact]
        public async Task GetAttendanceReport_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var report = new AttendanceReportDto();
            _mockAttendanceService.Setup(x => x.GetAttendanceReportAsync(1)).ReturnsAsync(report);

            var result = await _controller.GetAttendanceReport(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        // QR Code Tests
        [Fact]
        public async Task GetQrCode_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var qrCodeDto = new QrCodeImageDto { ImageBase64 = "base64image" };
            _mockQrCodeService.Setup(x => x.GenerateQrCodeImageAsync(1)).ReturnsAsync(qrCodeDto);

            var result = await _controller.GetQrCode(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(qrCodeDto, okResult.Value);
        }

        [Fact]
        public async Task RefreshQrCode_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Faculty");
            var qrCodeDto = new QrCodeImageDto { ImageBase64 = "newbase64image" };
            _mockQrCodeService.Setup(x => x.RefreshQrCodeAsync(1, 1)).ReturnsAsync("new-code-string");
            _mockQrCodeService.Setup(x => x.GenerateQrCodeImageAsync(1)).ReturnsAsync(qrCodeDto);

            var result = await _controller.RefreshQrCode(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(qrCodeDto, okResult.Value);
        }

        [Fact]
        public async Task CheckInWithQr_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Student");
            var dto = new QrCheckInRequestDto { QrCode = "valid-qr", Latitude = 41.0m, Longitude = 29.0m };
            var response = new QrCheckInResponseDto { Message = "Success", Success = true };
            
            _mockQrCodeService.Setup(x => x.CheckInWithQrAsync(1, 1, It.IsAny<QrCheckInRequestDto>(), It.IsAny<string>()))
                .ReturnsAsync(response);

            var result = await _controller.CheckInWithQr(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
