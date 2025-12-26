using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCampus.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // Academic, Attendance, System, etc.

        public bool IsRead { get; set; } = false;

        // Optional: Link to a specific object (e.g., CourseId, EventId)
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
    }
}
