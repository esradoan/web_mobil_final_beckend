namespace SmartCampus.Entities
{
    /// <summary>
    /// Yoklama oturumunu temsil eder.
    /// </summary>
    public class AttendanceSession : IEntity
    {
        public int Id { get; set; }
        
        public int SectionId { get; set; }
        public CourseSection? Section { get; set; }
        
        public int InstructorId { get; set; }
        public User? Instructor { get; set; }
        
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal GeofenceRadius { get; set; } = 15.0m;
        
        public string QrCode { get; set; } = string.Empty;
        public DateTime QrCodeExpiry { get; set; }
        
        public string Status { get; set; } = "active";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
        public ICollection<ExcuseRequest> ExcuseRequests { get; set; } = new List<ExcuseRequest>();
    }
}
