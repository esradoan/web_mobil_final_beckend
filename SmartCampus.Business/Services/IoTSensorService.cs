using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartCampus.Business.Services
{
    public interface IIoTSensorService
    {
        Task<IEnumerable<IoTSensor>> GetAllSensorsAsync();
        Task<IoTSensor?> GetSensorAsync(int id);
        Task<IEnumerable<SensorData>> GetSensorHistoryAsync(int sensorId, int count = 50);
        Task UpdateSensorValueAsync(int sensorId, double value);
        Task SimulateSensorsAsync(); // For demo purposes
    }

    public class IoTSensorService : IIoTSensorService
    {
        private readonly CampusDbContext _context;
        private readonly ISensorHubService _hubService;
        private readonly ILogger<IoTSensorService> _logger;

        public IoTSensorService(
            CampusDbContext context,
            ISensorHubService hubService,
            ILogger<IoTSensorService> logger)
        {
            _context = context;
            _hubService = hubService;
            _logger = logger;
        }

        public async Task<IEnumerable<IoTSensor>> GetAllSensorsAsync()
        {
            return await _context.IoTSensors
                .Include(s => s.Data.OrderByDescending(d => d.Timestamp).Take(1))
                .ToListAsync();
        }

        public async Task<IoTSensor?> GetSensorAsync(int id)
        {
            return await _context.IoTSensors
                .Include(s => s.Data.OrderByDescending(d => d.Timestamp).Take(10))
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SensorData>> GetSensorHistoryAsync(int sensorId, int count = 50)
        {
            return await _context.SensorData
                .Where(d => d.SensorId == sensorId)
                .OrderByDescending(d => d.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task UpdateSensorValueAsync(int sensorId, double value)
        {
            var sensor = await _context.IoTSensors.FindAsync(sensorId);
            if (sensor == null) return;

            // Update sensor status
            sensor.LastValue = value;
            sensor.LastUpdate = DateTime.UtcNow;

            // Record history
            var data = new SensorData
            {
                SensorId = sensorId,
                Value = value,
                Unit = "Generic", // Should be from sensor type
                Timestamp = DateTime.UtcNow
            };

            _context.SensorData.Add(data);
            await _context.SaveChangesAsync();

            // Broadcase via SignalR
            await _hubService.BroadcastSensorDataAsync(sensorId.ToString(), value, data.Timestamp);
        }

        public async Task SimulateSensorsAsync()
        {
            var sensors = await _context.IoTSensors.ToListAsync();
            if (!sensors.Any())
            {
                // Seed some sensors if none exist
                sensors.Add(new IoTSensor { Name = "Classroom 101 Temp", Type = "Temperature", Location = "Building A", LastValue = 22.5, LastUpdate = DateTime.UtcNow, IsActive = true });
                sensors.Add(new IoTSensor { Name = "Library Noise", Type = "Noise", Location = "Building B", LastValue = 45, LastUpdate = DateTime.UtcNow, IsActive = true });
                sensors.Add(new IoTSensor { Name = "Cafeteria Humidity", Type = "Humidity", Location = "Building C", LastValue = 60, LastUpdate = DateTime.UtcNow, IsActive = true });
                
                _context.IoTSensors.AddRange(sensors);
                await _context.SaveChangesAsync();
            }

            var random = new Random();

            foreach (var sensor in sensors)
            {
                if (!sensor.IsActive) continue;

                double newValue = sensor.LastValue.GetValueOrDefault(); // Start from last

                // Simulate random fluctuation
                if (sensor.Type == "Temperature")
                    newValue = 20 + (random.NextDouble() * 5); // 20-25
                else if (sensor.Type == "Noise")
                    newValue = 30 + (random.NextDouble() * 40); // 30-70
                else if (sensor.Type == "Humidity")
                    newValue = 40 + (random.NextDouble() * 30); // 40-70
                else
                    newValue += (random.NextDouble() * 2) - 1; // +/- 1

                await UpdateSensorValueAsync(sensor.Id, Math.Round(newValue, 1));
            }
        }
    }
}
