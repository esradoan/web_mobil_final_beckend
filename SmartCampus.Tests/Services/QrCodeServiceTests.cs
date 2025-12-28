using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class QrCodeServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly QrCodeService _service;

        public QrCodeServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _service = new QrCodeService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== GenerateQrCodeImageAsync Tests ====================

        [Fact]
        public async Task GenerateQrCodeImageAsync_ShouldReturnImage_WhenSessionExists()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "TEST123456",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GenerateQrCodeImageAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TEST123456", result.QrCode);
            Assert.NotEmpty(result.ImageBase64);
            Assert.Contains("data:image/png;base64,", result.ImageBase64);
        }

        [Fact]
        public async Task GenerateQrCodeImageAsync_ShouldThrow_WhenSessionNotFound()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GenerateQrCodeImageAsync(999));
        }

        // ==================== RefreshQrCodeAsync Tests ====================

        [Fact]
        public async Task RefreshQrCodeAsync_ShouldGenerateNewCode_WhenAuthorized()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1001,
                QrCode = "OLDCODE123",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RefreshQrCodeAsync(1, 1001);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual("OLDCODE123", result);
            Assert.Equal(12, result.Length);
        }

        [Fact]
        public async Task RefreshQrCodeAsync_ShouldThrow_WhenNotAuthorized()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1001,
                QrCode = "TEST123",
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RefreshQrCodeAsync(1, 9999));
        }

        [Fact]
        public async Task RefreshQrCodeAsync_ShouldThrow_WhenSessionNotActive()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1001,
                QrCode = "TEST123",
                Status = "closed",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RefreshQrCodeAsync(1, 1001));
        }

        // ==================== CheckInWithQrAsync Tests ====================

        [Fact]
        public async Task CheckInWithQrAsync_ShouldSucceed_WhenValidRequest()
        {
            // Arrange
            var student = new Student { Id = 1, UserId = 1001, IsActive = true, DepartmentId = 1 };
            _context.Students.Add(student);

            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "VALIDQR123",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date,
                Latitude = 41.0082m,
                Longitude = 29.0389m,
                GeofenceRadius = 500
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            var dto = new QrCheckInRequestDto
            {
                QrCode = "VALIDQR123",
                Latitude = 41.0082m,
                Longitude = 29.0389m,
                Accuracy = 50,
                DeviceType = "mobile"
            };

            // Act
            var result = await _service.CheckInWithQrAsync(1, 1001, dto, null);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Distance);
        }

        [Fact]
        public async Task CheckInWithQrAsync_ShouldFail_WhenStudentNotFound()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "VALIDQR123",
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            var dto = new QrCheckInRequestDto { QrCode = "VALIDQR123" };

            // Act
            var result = await _service.CheckInWithQrAsync(1, 9999, dto, null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("bulunamadı", result.Message);
        }

        [Fact]
        public async Task CheckInWithQrAsync_ShouldFail_WhenInvalidQrCode()
        {
            // Arrange
            var student = new Student { Id = 1, UserId = 1001, IsActive = true, DepartmentId = 1 };
            _context.Students.Add(student);

            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "CORRECTQR",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            var dto = new QrCheckInRequestDto { QrCode = "WRONGQR" };

            // Act
            var result = await _service.CheckInWithQrAsync(1, 1001, dto, null);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsFlagged);
        }

        [Fact]
        public async Task CheckInWithQrAsync_ShouldFail_WhenAlreadyCheckedIn()
        {
            // Arrange
            var student = new Student { Id = 1, UserId = 1001, IsActive = true, DepartmentId = 1 };
            _context.Students.Add(student);

            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "VALIDQR",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date,
                Latitude = 0,
                Longitude = 0,
                GeofenceRadius = 500
            };
            _context.AttendanceSessions.Add(session);

            var existingRecord = new AttendanceRecord { SessionId = 1, StudentId = 1001, CheckInTime = DateTime.UtcNow };
            _context.AttendanceRecords.Add(existingRecord);
            await _context.SaveChangesAsync();

            var dto = new QrCheckInRequestDto { QrCode = "VALIDQR" };

            // Act
            var result = await _service.CheckInWithQrAsync(1, 1001, dto, null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Already", result.Message);
        }

        // ==================== ValidateQrCodeAsync Tests ====================

        [Fact]
        public async Task ValidateQrCodeAsync_ShouldReturnTrue_WhenValid()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "VALIDCODE",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ValidateQrCodeAsync(1, "VALIDCODE");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateQrCodeAsync_ShouldReturnFalse_WhenExpired()
        {
            // Arrange
            var session = new AttendanceSession
            {
                Id = 1,
                SectionId = 1,
                InstructorId = 1,
                QrCode = "EXPIREDCODE",
                QrCodeExpiry = DateTime.UtcNow.AddMinutes(-5), // Expired
                Status = "active",
                Date = DateTime.UtcNow.Date
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ValidateQrCodeAsync(1, "EXPIREDCODE");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateQrCodeAsync_ShouldReturnFalse_WhenSessionNotFound()
        {
            // Act
            var result = await _service.ValidateQrCodeAsync(999, "ANYCODE");

            // Assert
            Assert.False(result);
        }
    }
}
