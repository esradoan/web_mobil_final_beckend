namespace SmartCampus.Business.Services
{
    public interface INotificationHubService
    {
        Task SendNotificationToUserAsync(string userId, object notification);
        Task SendNotificationToAllAsync(object notification);
    }
}
