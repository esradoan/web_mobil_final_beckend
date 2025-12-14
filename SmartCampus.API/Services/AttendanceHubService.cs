using Microsoft.AspNetCore.SignalR;
using SmartCampus.API.Hubs;

namespace SmartCampus.API.Services
{
    /// <summary>
    /// AttendanceHub'a mesaj göndermek için servis
    /// Business layer'dan çağrılabilir
    /// </summary>
    public interface IAttendanceHubService
    {
        /// <summary>
        /// Öğrenci check-in olduğunda bildirim gönder
        /// </summary>
        Task NotifyStudentCheckedInAsync(int sessionId, StudentCheckInNotification notification);
        
        /// <summary>
        /// Toplam yoklama sayısını güncelle
        /// </summary>
        Task NotifyAttendanceCountAsync(int sessionId, int attendedCount, int totalStudents);
        
        /// <summary>
        /// Oturum kapatıldığında bildirim gönder
        /// </summary>
        Task NotifySessionClosedAsync(int sessionId);
    }

    public class StudentCheckInNotification
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public decimal Distance { get; set; }
        public bool IsFlagged { get; set; }
        public string? FlagReason { get; set; }
    }

    public class AttendanceCountUpdate
    {
        public int SessionId { get; set; }
        public int AttendedCount { get; set; }
        public int TotalStudents { get; set; }
        public decimal Percentage { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AttendanceHubService : IAttendanceHubService
    {
        private readonly IHubContext<AttendanceHub> _hubContext;

        public AttendanceHubService(IHubContext<AttendanceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyStudentCheckedInAsync(int sessionId, StudentCheckInNotification notification)
        {
            var groupName = $"session_{sessionId}";
            await _hubContext.Clients.Group(groupName)
                .SendAsync(AttendanceHubMessages.StudentCheckedIn, notification);
        }

        public async Task NotifyAttendanceCountAsync(int sessionId, int attendedCount, int totalStudents)
        {
            var groupName = $"session_{sessionId}";
            var update = new AttendanceCountUpdate
            {
                SessionId = sessionId,
                AttendedCount = attendedCount,
                TotalStudents = totalStudents,
                Percentage = totalStudents > 0 ? Math.Round((decimal)attendedCount / totalStudents * 100, 1) : 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group(groupName)
                .SendAsync(AttendanceHubMessages.AttendanceCountUpdated, update);
        }

        public async Task NotifySessionClosedAsync(int sessionId)
        {
            var groupName = $"session_{sessionId}";
            await _hubContext.Clients.Group(groupName)
                .SendAsync(AttendanceHubMessages.SessionClosed, new { sessionId, closedAt = DateTime.UtcNow });
        }
    }
}
