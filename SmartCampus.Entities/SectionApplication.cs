namespace SmartCampus.Entities
{
    /// <summary>
    /// Öğretmenlerin derslere yaptığı başvuruları temsil eder.
    /// </summary>
    public class CourseApplication : BaseEntity
    {
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        
        public int InstructorId { get; set; }
        public User? Instructor { get; set; }
        
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; } // Admin UserId
        public User? ProcessedByUser { get; set; }
        
        public string? RejectionReason { get; set; }
    }
    
    public enum ApplicationStatus
    {
        Pending = 0,    // Beklemede
        Approved = 1,   // Onaylandı
        Rejected = 2   // Reddedildi
    }
}

