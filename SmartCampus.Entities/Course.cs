namespace SmartCampus.Entities
{
    /// <summary>
    /// Ders bilgilerini temsil eder.
    /// </summary>
    public class Course : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Ects { get; set; }
        public string? SyllabusUrl { get; set; }
        
        // İlişkiler
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
        
        // Navigation Properties
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        public ICollection<CoursePrerequisite> PrerequisiteFor { get; set; } = new List<CoursePrerequisite>();
        public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
    }
}
