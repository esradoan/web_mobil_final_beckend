using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace SMARTCAMPUS.Tests.Controllers
{
    public class IoTSensorsControllerTests
    {
        private readonly Mock<IIoTSensorService> _mockService;
        private readonly IoTSensorsController _controller;

        public IoTSensorsControllerTests()
        {
            _mockService = new Mock<IIoTSensorService>();
            _controller = new IoTSensorsController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllSensors_ReturnsOk_WithList()
        {
            // Arrange
            var sensors = new List<IoTSensor>
            {
                new IoTSensor { Id = 1, Name = "Temp Sensor" }
            };
            _mockService.Setup(x => x.GetAllSensorsAsync()).ReturnsAsync(sensors);

            // Act
            var result = await _controller.GetAllSensors();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnList = Assert.IsType<List<IoTSensor>>(okResult.Value);
            Assert.Single(returnList);
        }

        [Fact]
        public async Task TriggerSimulation_ReturnsOk()
        {
            // Act
            var result = await _controller.TriggerSimulation();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockService.Verify(x => x.SimulateSensorsAsync(), Times.Once);
        }
    }
}
