using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace SmartCampus.API.Hubs
{
    /// <summary>
    /// Real-time yoklama takip hub'ı
    /// Faculty: Oturum başlatınca bağlanır, anlık check-in bilgisi alır
    /// </summary>
    [Authorize]
    public class AttendanceHub : Hub
    {
        /// <summary>
        /// Instructor bir oturumu izlemeye başladığında çağrılır
        /// </summary>
        public async Task JoinSession(int sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            await Clients.Caller.SendAsync("JoinedSession", new { sessionId, message = "Now watching session" });
        }

        /// <summary>
        /// Oturumu izlemeyi bırakır
        /// </summary>
        public async Task LeaveSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            await Clients.Caller.SendAsync("LeftSession", new { sessionId });
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", new { 
                connectionId = Context.ConnectionId,
                message = "Connected to attendance hub" 
            });
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// SignalR üzerinden gönderilecek mesaj tipleri
    /// </summary>
    public static class AttendanceHubMessages
    {
        public const string StudentCheckedIn = "StudentCheckedIn";
        public const string AttendanceCountUpdated = "AttendanceCountUpdated";
        public const string SessionClosed = "SessionClosed";
    }
}
