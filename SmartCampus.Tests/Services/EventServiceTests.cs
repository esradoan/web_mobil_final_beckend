using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class EventServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly EventService _service;
        private readonly Mock<IWalletService> _walletServiceMock;

        public EventServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _walletServiceMock = new Mock<IWalletService>();
            _service = new EventService(_context, _walletServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== GetEventsAsync Tests ====================

        [Fact]
        public async Task GetEventsAsync_ShouldReturnPublishedEvents()
        {
            // Arrange
            var organizer = new User { Id = 1, FirstName = "Admin", LastName = "User" };
            _context.Users.Add(organizer);

            _context.Events.Add(new Event { Id = 1, Title = "Event1", Status = "published", OrganizerId = 1, Date = DateTime.UtcNow.AddDays(1) });
            _context.Events.Add(new Event { Id = 2, Title = "Event2", Status = "cancelled", OrganizerId = 1, Date = DateTime.UtcNow.AddDays(2) });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Event1", result[0].Title);
        }

        [Fact]
        public async Task GetEventsAsync_ShouldFilterByCategory()
        {
            // Arrange
            var organizer = new User { Id = 1, FirstName = "Admin", LastName = "User" };
            _context.Users.Add(organizer);

            _context.Events.Add(new Event { Id = 1, Title = "Social Event", Category = "social", Status = "published", OrganizerId = 1, Date = DateTime.UtcNow });
            _context.Events.Add(new Event { Id = 2, Title = "Academic Event", Category = "academic", Status = "published", OrganizerId = 1, Date = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetEventsAsync(category: "social");

            // Assert
            Assert.Single(result);
            Assert.Equal("Social Event", result[0].Title);
        }

        // ==================== GetEventByIdAsync Tests ====================

        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnEvent_WhenExists()
        {
            // Arrange
            var organizer = new User { Id = 1, FirstName = "Admin", LastName = "User" };
            _context.Users.Add(organizer);

            var evt = new Event { Id = 1, Title = "Test Event", Status = "published", OrganizerId = 1, Date = DateTime.UtcNow };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetEventByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Event", result.Title);
        }

        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetEventByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        // ==================== CreateEventAsync Tests ====================

        [Fact]
        public async Task CreateEventAsync_ShouldCreateEvent()
        {
            // Arrange
            var organizerId = 1;
            var dto = new CreateEventDto
            {
                Title = "New Event",
                Description = "Test description",
                Category = "social",
                Date = DateTime.UtcNow.AddDays(7),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12),
                Location = "Room A",
                Capacity = 100,
                RegistrationDeadline = DateTime.UtcNow.AddDays(5),
                IsPaid = false,
                Price = 0
            };

            // Act
            var result = await _service.CreateEventAsync(organizerId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Event", result.Title);
            Assert.Equal("published", result.Status);

            var dbEvent = await _context.Events.FindAsync(result.Id);
            Assert.NotNull(dbEvent);
        }

        // ==================== UpdateEventAsync Tests ====================

        [Fact]
        public async Task UpdateEventAsync_ShouldUpdateEvent_WhenExists()
        {
            // Arrange
            var organizer = new User { Id = 1, FirstName = "Admin", LastName = "User" };
            _context.Users.Add(organizer);

            var evt = new Event { Id = 1, Title = "Old Title", Status = "published", OrganizerId = 1, Date = DateTime.UtcNow };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateEventDto { Title = "New Title" };

            // Act
            var result = await _service.UpdateEventAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Title", result.Title);
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.UpdateEventAsync(999, new UpdateEventDto { Title = "X" });

            // Assert
            Assert.Null(result);
        }

        // ==================== DeleteEventAsync Tests ====================

        [Fact]
        public async Task DeleteEventAsync_ShouldCancelEvent_WhenExists()
        {
            // Arrange
            var evt = new Event { Id = 1, Title = "Event", Status = "published", Date = DateTime.UtcNow };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteEventAsync(1);

            // Assert
            Assert.True(result);

            var dbEvent = await _context.Events.FindAsync(1);
            Assert.Equal("cancelled", dbEvent!.Status);
        }

        [Fact]
        public async Task DeleteEventAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Act
            var result = await _service.DeleteEventAsync(999);

            // Assert
            Assert.False(result);
        }

        // ==================== RegisterAsync Tests ====================

        [Fact]
        public async Task RegisterAsync_ShouldRegisterUser_WhenValid()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);

            var evt = new Event
            {
                Id = 1,
                Title = "Test Event",
                Status = "published",
                Capacity = 100,
                RegisteredCount = 0,
                RegistrationDeadline = DateTime.UtcNow.AddDays(5),
                IsPaid = false,
                Date = DateTime.UtcNow.AddDays(7)
            };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RegisterAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("registered", result.Status);
            Assert.False(result.CheckedIn);
            Assert.NotEmpty(result.QrCode);

            var dbEvent = await _context.Events.FindAsync(1);
            Assert.Equal(1, dbEvent!.RegisteredCount);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenEventNotFound()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(1, 999));
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenEventFull()
        {
            // Arrange
            var evt = new Event
            {
                Id = 1,
                Title = "Full Event",
                Status = "published",
                Capacity = 1,
                RegisteredCount = 1,
                RegistrationDeadline = DateTime.UtcNow.AddDays(5),
                Date = DateTime.UtcNow.AddDays(7)
            };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(1, 1));
            Assert.Contains("full", ex.Message);
        }

        // ==================== CancelRegistrationAsync Tests ====================

        [Fact]
        public async Task CancelRegistrationAsync_ShouldCancel_WhenValid()
        {
            // Arrange
            var evt = new Event { Id = 1, Title = "Event", Status = "published", RegisteredCount = 1, Date = DateTime.UtcNow.AddDays(7) };
            _context.Events.Add(evt);

            var reg = new EventRegistration { Id = 1, EventId = 1, UserId = 1, Status = "registered", QrCode = "TEST123" };
            _context.EventRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CancelRegistrationAsync(1, 1);

            // Assert
            Assert.True(result);

            var dbReg = await _context.EventRegistrations.FindAsync(1);
            Assert.Equal("cancelled", dbReg!.Status);
        }

        [Fact]
        public async Task CancelRegistrationAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Act
            var result = await _service.CancelRegistrationAsync(1, 999);

            // Assert
            Assert.False(result);
        }

        // ==================== GetMyRegistrationsAsync Tests ====================

        [Fact]
        public async Task GetMyRegistrationsAsync_ShouldReturnUserRegistrations()
        {
            // Arrange
            var evt = new Event { Id = 1, Title = "Event", Status = "published", Date = DateTime.UtcNow.AddDays(7) };
            _context.Events.Add(evt);

            var reg = new EventRegistration { Id = 1, EventId = 1, UserId = 1, Status = "registered", QrCode = "ABC123", RegistrationDate = DateTime.UtcNow };
            _context.EventRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMyRegistrationsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        // ==================== CheckInAsync Tests ====================

        [Fact]
        public async Task CheckInAsync_ShouldCheckIn_WhenValidQrCode()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);

            var evt = new Event { Id = 1, Title = "Event", Status = "published", Date = DateTime.UtcNow, IsPaid = false };
            _context.Events.Add(evt);

            var qrCode = "VALIDQR123";
            var reg = new EventRegistration { Id = 1, EventId = 1, UserId = 1, Status = "registered", QrCode = qrCode, CheckedIn = false };
            _context.EventRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckInAsync(1, qrCode);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CheckedIn);
            Assert.NotNull(result.CheckedInAt);
        }

        [Fact]
        public async Task CheckInAsync_ShouldThrow_WhenInvalidQrCode()
        {
            // Arrange
            var evt = new Event { Id = 1, Title = "Event", Status = "published", Date = DateTime.UtcNow };
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.CheckInAsync(1, "INVALIDQR"));
            Assert.Contains("Invalid QR", ex.Message);
        }
    }
}
