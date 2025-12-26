using Microsoft.AspNetCore.SignalR;
using SmartCampus.API.Hubs;
using SmartCampus.Business.Services;

namespace SmartCampus.API.Services
{
    public class SensorHubService : ISensorHubService
    {
        private readonly IHubContext<SensorHub> _hubContext;

        public SensorHubService(IHubContext<SensorHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastSensorDataAsync(string sensorId, double value, DateTime timestamp)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveSensorData", sensorId, value, timestamp);
        }
    }
}
