namespace SmartCampus.Entities
{
    /// <summary>
    /// Öğrenci ders kayıtlarını ve notlarını temsil eder.
    /// </summary>
    public class Enrollment : IEntity
    {
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        public User? Student { get; set; }
        
        public int SectionId { get; set; }
        public CourseSection? Section { get; set; }
        
        public string Status { get; set; } = "enrolled";
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
        
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
