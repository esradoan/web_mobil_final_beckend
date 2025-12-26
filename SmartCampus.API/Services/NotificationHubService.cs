using Microsoft.AspNetCore.SignalR;
using SmartCampus.API.Hubs;
using SmartCampus.Business.Services;

namespace SmartCampus.API.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationHubService> _logger;

        public NotificationHubService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendNotificationToUserAsync(string userId, object notification)
        {
            try
            {
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"SignalR notification sent to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SignalR notification to user {userId}");
            }
        }

        public async Task SendNotificationToAllAsync(object notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveBroadcast", notification);
                _logger.LogInformation("SignalR broadcast sent to all users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR broadcast");
            }
        }
    }
}
