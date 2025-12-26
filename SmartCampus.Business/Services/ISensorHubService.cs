namespace SmartCampus.Business.Services
{
    public interface ISensorHubService
    {
        Task BroadcastSensorDataAsync(string sensorId, double value, DateTime timestamp);
    }
}
