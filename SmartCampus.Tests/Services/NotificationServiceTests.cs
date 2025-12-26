using Moq;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SMARTCAMPUS.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<INotificationHubService> _mockHub;
        private readonly Mock<ILogger<NotificationService>> _logger;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _mockEmail = new Mock<IEmailService>();
            _mockHub = new Mock<INotificationHubService>();
            _logger = new Mock<ILogger<NotificationService>>();

            _service = new NotificationService(_context, _mockEmail.Object, _mockHub.Object, _logger.Object);
        }

        [Fact]
        public async Task SendNotificationAsync_SavesToDb_And_Broadcasts()
        {
            // Arrange
            int userId = 10;
            string title = "Hello";
            string msg = "World";

            // Act
            await _service.SendNotificationAsync(userId, title, msg, "General");

            // Assert
            var saved = await _context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal(title, saved.Title);
            Assert.Equal(userId, saved.UserId);

            // Verify Hub Call
            _mockHub.Verify(x => x.SendNotificationToUserAsync(It.Is<string>(s => s == userId.ToString()), It.IsAny<object>()), Times.Once);

            // Verify Email (assuming preference check implementation details allow simple pass or default true)
            // But SendNotificationAsync checks DB for preferences. DB is empty, so default true.
            // But we need a User in DB with email for email service to actually be called.
        }

        [Fact]
        public async Task GetUserNotifications_ReturnsOnlyUserItems()
        {
            // Arrange
            _context.Notifications.Add(new Notification { UserId = 1, Title = "My Msg" });
            _context.Notifications.Add(new Notification { UserId = 2, Title = "Other Msg" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserNotificationsAsync(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("My Msg", result.First().Title);
        }
    }
}
