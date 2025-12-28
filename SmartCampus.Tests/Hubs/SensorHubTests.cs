using Microsoft.AspNetCore.SignalR;
using Moq;
using SmartCampus.API.Hubs;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Hubs
{
    public class SensorHubTests
    {
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly SensorHub _hub;

        public SensorHubTests()
        {
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _hub = new SensorHub
            {
                Clients = _mockClients.Object
            };
        }

        [Fact]
        public async Task SendSensorData_ShouldBroadcastToAll()
        {
            // Arrange
            var sensorId = "TEMP_001";
            var value = 23.5;

            // Act
            await _hub.SendSensorData(sensorId, value);

            // Assert
            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveSensorData",
                    It.Is<object[]>(o => o.Length == 2 && (string)o[0] == sensorId && (double)o[1] == value),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendSensorData_WithDifferentValues_ShouldSendCorrectData()
        {
            // Arrange
            var sensorId = "HUMIDITY_005";
            var value = 78.2;

            // Act
            await _hub.SendSensorData(sensorId, value);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveSensorData",
                    It.Is<object[]>(o => (string)o[0] == "HUMIDITY_005" && (double)o[1] == 78.2),
                    default),
                Times.Once);
        }
    }
}
