using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SmartCampus.API.Hubs
{
    public class SensorHub : Hub
    {
        public async Task SendSensorData(string sensorId, double value)
        {
            await Clients.All.SendAsync("ReceiveSensorData", sensorId, value);
        }
    }
}
