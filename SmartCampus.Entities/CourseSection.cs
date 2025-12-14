namespace SmartCampus.Entities
{
    /// <summary>
    /// Ders şubelerini temsil eder.
    /// </summary>
    public class CourseSection : BaseEntity
    {
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        
        public string SectionNumber { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public int Year { get; set; }
        
        public int InstructorId { get; set; }
        public User? Instructor { get; set; }
        
        public int Capacity { get; set; } = 50;
        public int EnrolledCount { get; set; } = 0;
        
        public string? ScheduleJson { get; set; }
        
        public int? ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }
        
        // Navigation Properties
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }
}
