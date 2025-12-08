using System;

namespace SmartCampus.Entities
{
    public class UserActivityLog : BaseEntity
    {
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Keep explicit timestamp for log or map to CreatedAt
    }
}
