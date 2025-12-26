using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/sensors")]
    [Authorize] // Maybe AllowAnonymous for some if public dashboard? Default Authorize.
    public class IoTSensorsController : ControllerBase
    {
        private readonly IIoTSensorService _sensorService;

        public IoTSensorsController(IIoTSensorService sensorService)
        {
            _sensorService = sensorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSensors()
        {
            var sensors = await _sensorService.GetAllSensorsAsync();
            return Ok(sensors);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSensor(int id)
        {
            var sensor = await _sensorService.GetSensorAsync(id);
            if (sensor == null) return NotFound();
            return Ok(sensor);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetSensorHistory(int id)
        {
            var history = await _sensorService.GetSensorHistoryAsync(id);
            return Ok(history);
        }

        [HttpPost("simulate")]
        public async Task<IActionResult> TriggerSimulation()
        {
            await _sensorService.SimulateSensorsAsync();
            return Ok(new { message = "Simulation triggered. Sensor values updated." });
        }
    }
}
