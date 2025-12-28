using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class IoTSensorsControllerTests
    {
        private readonly Mock<IIoTSensorService> _mockSensorService;
        private readonly IoTSensorsController _controller;

        public IoTSensorsControllerTests()
        {
            _mockSensorService = new Mock<IIoTSensorService>();
            _controller = new IoTSensorsController(_mockSensorService.Object);
        }

        [Fact]
        public async Task GetAllSensors_Success_ReturnsOk()
        {
            // Arrange
            _mockSensorService.Setup(s => s.GetAllSensorsAsync())
                .ReturnsAsync(new List<IoTSensor>());

            // Act
            var result = await _controller.GetAllSensors();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSensor_Success_ReturnsOk()
        {
            // Arrange
            _mockSensorService.Setup(s => s.GetSensorAsync(1))
                .ReturnsAsync(new IoTSensor { Id = 1 });

            // Act
            var result = await _controller.GetSensor(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSensor_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockSensorService.Setup(s => s.GetSensorAsync(999))
                .ReturnsAsync((IoTSensor?)null);

            // Act
            var result = await _controller.GetSensor(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetSensorHistory_Success_ReturnsOk()
        {
            // Arrange
            _mockSensorService.Setup(s => s.GetSensorHistoryAsync(1, 50))
                .ReturnsAsync(new List<SensorData>());

            // Act
            var result = await _controller.GetSensorHistory(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task TriggerSimulation_Success_ReturnsOk()
        {
            // Arrange
            _mockSensorService.Setup(s => s.SimulateSensorsAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.TriggerSimulation();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
