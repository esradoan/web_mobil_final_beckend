using Microsoft.AspNetCore.SignalR;
using Moq;
using SmartCampus.API.Hubs;
using SmartCampus.API.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class AttendanceHubServiceTests
    {
        private readonly Mock<IHubContext<AttendanceHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly AttendanceHubService _service;

        public AttendanceHubServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<AttendanceHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _service = new AttendanceHubService(_mockHubContext.Object);
        }

        [Fact]
        public async Task NotifyStudentCheckedInAsync_ShouldSendToCorrectGroup()
        {
            // Arrange
            var notification = new StudentCheckInNotification
            {
                StudentId = 1001,
                StudentName = "Test Student",
                StudentNumber = "12345",
                CheckInTime = DateTime.UtcNow,
                Distance = 10.5m,
                IsFlagged = false
            };

            // Act
            await _service.NotifyStudentCheckedInAsync(1, notification);

            // Assert
            _mockClients.Verify(c => c.Group("session_1"), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    AttendanceHubMessages.StudentCheckedIn,
                    It.Is<object[]>(o => o.Length == 1 && o[0] == notification),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAttendanceCountAsync_ShouldCalculatePercentageCorrectly()
        {
            // Arrange
            AttendanceCountUpdate? capturedUpdate = null;
            _mockClientProxy.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default))
                .Callback<string, object[], System.Threading.CancellationToken>((method, args, token) =>
                {
                    capturedUpdate = args[0] as AttendanceCountUpdate;
                });

            // Act
            await _service.NotifyAttendanceCountAsync(1, 25, 30);

            // Assert
            Assert.NotNull(capturedUpdate);
            Assert.Equal(1, capturedUpdate.SessionId);
            Assert.Equal(25, capturedUpdate.AttendedCount);
            Assert.Equal(30, capturedUpdate.TotalStudents);
            Assert.Equal(83.3m, capturedUpdate.Percentage);
        }

        [Fact]
        public async Task NotifyAttendanceCountAsync_WithZeroTotal_ShouldReturnZeroPercentage()
        {
            // Arrange
            AttendanceCountUpdate? capturedUpdate = null;
            _mockClientProxy.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default))
                .Callback<string, object[], System.Threading.CancellationToken>((method, args, token) =>
                {
                    capturedUpdate = args[0] as AttendanceCountUpdate;
                });

            // Act
            await _service.NotifyAttendanceCountAsync(1, 0, 0);

            // Assert
            Assert.NotNull(capturedUpdate);
            Assert.Equal(0m, capturedUpdate.Percentage);
        }

        [Fact]
        public async Task NotifySessionClosedAsync_ShouldSendToCorrectGroup()
        {
            // Act
            await _service.NotifySessionClosedAsync(1);

            // Assert
            _mockClients.Verify(c => c.Group("session_1"), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    AttendanceHubMessages.SessionClosed,
                    It.Is<object[]>(o => o.Length == 1),
                    default),
                Times.Once);
        }
    }
}
