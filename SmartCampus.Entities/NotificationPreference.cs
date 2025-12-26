using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCampus.Entities
{
    public class NotificationPreference : BaseEntity
    {
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public bool EmailEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;

        // Granular preferences
        public bool AcademicNotifications { get; set; } = true;
        public bool AttendanceNotifications { get; set; } = true;
        public bool MealNotifications { get; set; } = true;
        public bool EventNotifications { get; set; } = true;
    }
}
