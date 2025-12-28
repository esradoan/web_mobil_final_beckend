using Microsoft.AspNetCore.SignalR;
using Moq;
using SmartCampus.API.Hubs;
using SmartCampus.API.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class SensorHubServiceTests
    {
        private readonly Mock<IHubContext<SensorHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly SensorHubService _service;

        public SensorHubServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<SensorHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _service = new SensorHubService(_mockHubContext.Object);
        }

        [Fact]
        public async Task BroadcastSensorDataAsync_ShouldSendToAllClients()
        {
            // Arrange
            var sensorId = "TEMP_001";
            var value = 25.5;
            var timestamp = DateTime.UtcNow;

            // Act
            await _service.BroadcastSensorDataAsync(sensorId, value, timestamp);

            // Assert
            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveSensorData",
                    It.Is<object[]>(o => 
                        o.Length == 3 && 
                        (string)o[0] == sensorId && 
                        (double)o[1] == value &&
                        (DateTime)o[2] == timestamp),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastSensorDataAsync_WithDifferentSensor_ShouldSendCorrectData()
        {
            // Arrange
            var sensorId = "HUMIDITY_002";
            var value = 65.0;
            var timestamp = DateTime.UtcNow;

            // Act
            await _service.BroadcastSensorDataAsync(sensorId, value, timestamp);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveSensorData",
                    It.Is<object[]>(o => (string)o[0] == "HUMIDITY_002" && (double)o[1] == 65.0),
                    default),
                Times.Once);
        }
    }
}
