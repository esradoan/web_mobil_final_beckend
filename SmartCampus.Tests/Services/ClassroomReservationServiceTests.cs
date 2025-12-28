using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class ClassroomReservationServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly ClassroomReservationService _service;

        public ClassroomReservationServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _service = new ClassroomReservationService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== CreateReservationAsync Tests ====================

        [Fact]
        public async Task CreateReservationAsync_ShouldCreateReservation_WhenValid()
        {
            // Arrange
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync();

            var dto = new CreateClassroomReservationDto
            {
                ClassroomId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12),
                Purpose = "Meeting"
            };

            // Act
            var result = await _service.CreateReservationAsync(1001, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("pending", result.Status);
            Assert.Equal("Meeting", result.Purpose);
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldThrow_WhenClassroomNotFound()
        {
            // Arrange
            var dto = new CreateClassroomReservationDto
            {
                ClassroomId = 999,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12),
                Purpose = "Meeting"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateReservationAsync(1001, dto));
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldThrow_WhenInvalidTimeRange()
        {
            // Arrange
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync();

            var dto = new CreateClassroomReservationDto
            {
                ClassroomId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.FromHours(14),
                EndTime = TimeSpan.FromHours(12), // End before start
                Purpose = "Meeting"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateReservationAsync(1001, dto));
            Assert.Contains("End time", ex.Message);
        }

        // ==================== GetReservationByIdAsync Tests ====================

        [Fact]
        public async Task GetReservationByIdAsync_ShouldReturnReservation_WhenExists()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Users.Add(user);
            _context.Classrooms.Add(classroom);

            var reservation = new ClassroomReservation
            {
                Id = 1,
                ClassroomId = 1,
                UserId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12),
                Purpose = "Meeting",
                Status = "pending"
            };
            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetReservationByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Meeting", result.Purpose);
        }

        [Fact]
        public async Task GetReservationByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetReservationByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        // ==================== GetMyReservationsAsync Tests ====================

        [Fact]
        public async Task GetMyReservationsAsync_ShouldReturnUserReservations()
        {
            // Arrange
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Classrooms.Add(classroom);

            _context.ClassroomReservations.Add(new ClassroomReservation { Id = 1, ClassroomId = 1, UserId = 1001, Date = DateTime.UtcNow, Status = "pending", Purpose = "A" });
            _context.ClassroomReservations.Add(new ClassroomReservation { Id = 2, ClassroomId = 1, UserId = 1002, Date = DateTime.UtcNow, Status = "pending", Purpose = "B" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMyReservationsAsync(1001);

            // Assert
            Assert.Single(result);
        }

        // ==================== CancelReservationAsync Tests ====================

        [Fact]
        public async Task CancelReservationAsync_ShouldCancel_WhenValid()
        {
            // Arrange
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Classrooms.Add(classroom);

            var reservation = new ClassroomReservation
            {
                Id = 1,
                ClassroomId = 1,
                UserId = 1001,
                Date = DateTime.UtcNow.AddDays(1),
                Status = "pending",
                Purpose = "Meeting"
            };
            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CancelReservationAsync(1001, 1);

            // Assert
            Assert.True(result);

            var dbRes = await _context.ClassroomReservations.FindAsync(1);
            Assert.Equal("cancelled", dbRes!.Status);
        }

        [Fact]
        public async Task CancelReservationAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Act
            var result = await _service.CancelReservationAsync(1001, 999);

            // Assert
            Assert.False(result);
        }

        // ==================== ApproveReservationAsync Tests ====================

        [Fact]
        public async Task ApproveReservationAsync_ShouldApprove_WhenPending()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Users.Add(user);
            _context.Classrooms.Add(classroom);

            var reservation = new ClassroomReservation
            {
                Id = 1,
                ClassroomId = 1,
                UserId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                Status = "pending",
                Purpose = "Meeting"
            };
            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ApproveReservationAsync(adminUserId: 2, reservationId: 1, notes: "Approved");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("approved", result.Status);
        }

        [Fact]
        public async Task ApproveReservationAsync_ShouldThrow_WhenNotPending()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Users.Add(user);
            _context.Classrooms.Add(classroom);

            var reservation = new ClassroomReservation
            {
                Id = 1,
                ClassroomId = 1,
                UserId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                Status = "approved", // Already approved
                Purpose = "Meeting"
            };
            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.ApproveReservationAsync(2, 1));
        }

        // ==================== RejectReservationAsync Tests ====================

        [Fact]
        public async Task RejectReservationAsync_ShouldReject_WhenPending()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Users.Add(user);
            _context.Classrooms.Add(classroom);

            var reservation = new ClassroomReservation
            {
                Id = 1,
                ClassroomId = 1,
                UserId = 1,
                Date = DateTime.UtcNow.AddDays(1),
                Status = "pending",
                Purpose = "Meeting"
            };
            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RejectReservationAsync(adminUserId: 2, reservationId: 1, notes: "No space");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("rejected", result.Status);
        }

        // ==================== GetPendingReservationsAsync Tests ====================

        [Fact]
        public async Task GetPendingReservationsAsync_ShouldReturnOnlyPending()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);
            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 };
            _context.Classrooms.Add(classroom);

            _context.ClassroomReservations.Add(new ClassroomReservation { Id = 1, ClassroomId = 1, UserId = 1, Date = DateTime.UtcNow.AddDays(1), Status = "pending", Purpose = "A" });
            _context.ClassroomReservations.Add(new ClassroomReservation { Id = 2, ClassroomId = 1, UserId = 1, Date = DateTime.UtcNow.AddDays(1), Status = "approved", Purpose = "B" });
            _context.ClassroomReservations.Add(new ClassroomReservation { Id = 3, ClassroomId = 1, UserId = 1, Date = DateTime.UtcNow.AddDays(1), Status = "pending", Purpose = "C" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetPendingReservationsAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        // ==================== GetAvailableClassroomsAsync Tests ====================

        [Fact]
        public async Task GetAvailableClassroomsAsync_ShouldReturnAvailableRooms()
        {
            // Arrange
            _context.Classrooms.Add(new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 50 });
            _context.Classrooms.Add(new Classroom { Id = 2, Building = "A", RoomNumber = "102", Capacity = 30 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAvailableClassroomsAsync(
                date: DateTime.UtcNow.AddDays(1),
                startTime: TimeSpan.FromHours(10),
                endTime: TimeSpan.FromHours(12)
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }
    }
}
