using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class IoTSensorServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly IoTSensorService _service;
        private readonly Mock<ISensorHubService> _hubServiceMock;
        private readonly Mock<ILogger<IoTSensorService>> _loggerMock;

        public IoTSensorServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _hubServiceMock = new Mock<ISensorHubService>();
            _loggerMock = new Mock<ILogger<IoTSensorService>>();
            _service = new IoTSensorService(_context, _hubServiceMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== GetAllSensorsAsync Tests ====================

        [Fact]
        public async Task GetAllSensorsAsync_ShouldReturnAllSensors()
        {
            // Arrange
            _context.IoTSensors.Add(new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true });
            _context.IoTSensors.Add(new IoTSensor { Id = 2, Name = "Sensor2", Type = "Humidity", Location = "Room2", IsActive = true });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllSensorsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllSensorsAsync_ShouldReturnEmpty_WhenNoSensors()
        {
            // Act
            var result = await _service.GetAllSensorsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ==================== GetSensorAsync Tests ====================

        [Fact]
        public async Task GetSensorAsync_ShouldReturnSensor_WhenExists()
        {
            // Arrange
            var sensor = new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true };
            _context.IoTSensors.Add(sensor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSensorAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Sensor1", result.Name);
        }

        [Fact]
        public async Task GetSensorAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetSensorAsync(999);

            // Assert
            Assert.Null(result);
        }

        // ==================== GetSensorHistoryAsync Tests ====================

        [Fact]
        public async Task GetSensorHistoryAsync_ShouldReturnHistory()
        {
            // Arrange
            var sensor = new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true };
            _context.IoTSensors.Add(sensor);

            _context.SensorData.Add(new SensorData { SensorId = 1, Value = 22.5, Timestamp = DateTime.UtcNow.AddMinutes(-10) });
            _context.SensorData.Add(new SensorData { SensorId = 1, Value = 23.0, Timestamp = DateTime.UtcNow.AddMinutes(-5) });
            _context.SensorData.Add(new SensorData { SensorId = 1, Value = 23.5, Timestamp = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSensorHistoryAsync(1, count: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetSensorHistoryAsync_ShouldLimitResults()
        {
            // Arrange
            var sensor = new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true };
            _context.IoTSensors.Add(sensor);

            for (int i = 0; i < 100; i++)
            {
                _context.SensorData.Add(new SensorData { SensorId = 1, Value = 20 + i * 0.1, Timestamp = DateTime.UtcNow.AddMinutes(-i) });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSensorHistoryAsync(1, count: 10);

            // Assert
            Assert.Equal(10, result.Count());
        }

        // ==================== UpdateSensorValueAsync Tests ====================

        [Fact]
        public async Task UpdateSensorValueAsync_ShouldUpdateValue_WhenSensorExists()
        {
            // Arrange
            var sensor = new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true, LastValue = 20 };
            _context.IoTSensors.Add(sensor);
            await _context.SaveChangesAsync();

            _hubServiceMock.Setup(h => h.BroadcastSensorDataAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateSensorValueAsync(1, 25.0);

            // Assert
            var updated = await _context.IoTSensors.FindAsync(1);
            Assert.Equal(25.0, updated!.LastValue);

            var history = await _context.SensorData.Where(d => d.SensorId == 1).ToListAsync();
            Assert.Single(history);
            Assert.Equal(25.0, history[0].Value);
        }

        [Fact]
        public async Task UpdateSensorValueAsync_ShouldDoNothing_WhenSensorNotExists()
        {
            // Act
            await _service.UpdateSensorValueAsync(999, 25.0);

            // Assert - no exception, no data added
            var history = await _context.SensorData.ToListAsync();
            Assert.Empty(history);
        }

        // ==================== SimulateSensorsAsync Tests ====================

        [Fact]
        public async Task SimulateSensorsAsync_ShouldCreateSensors_WhenNoneExist()
        {
            // Arrange
            _hubServiceMock.Setup(h => h.BroadcastSensorDataAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SimulateSensorsAsync();

            // Assert
            var sensors = await _context.IoTSensors.ToListAsync();
            Assert.Equal(3, sensors.Count); // Seeds 3 sensors
        }

        [Fact]
        public async Task SimulateSensorsAsync_ShouldUpdateValues_WhenSensorsExist()
        {
            // Arrange
            var sensor = new IoTSensor { Id = 1, Name = "Sensor1", Type = "Temperature", Location = "Room1", IsActive = true, LastValue = 20 };
            _context.IoTSensors.Add(sensor);
            await _context.SaveChangesAsync();

            _hubServiceMock.Setup(h => h.BroadcastSensorDataAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SimulateSensorsAsync();

            // Assert
            var history = await _context.SensorData.Where(d => d.SensorId == 1).ToListAsync();
            Assert.Single(history);
        }
    }
}
