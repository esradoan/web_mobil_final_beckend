namespace SmartCampus.Entities
{
    /// <summary>
    /// Öğrencilerin derslere yaptığı başvuruları temsil eder.
    /// </summary>
    public class StudentCourseApplication : BaseEntity
    {
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        
        public int SectionId { get; set; }
        public CourseSection? Section { get; set; }
        
        public int StudentId { get; set; }
        public User? Student { get; set; }
        
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; } // Admin UserId
        public User? ProcessedByUser { get; set; }
        
        public string? RejectionReason { get; set; }
    }
}

