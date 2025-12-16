namespace SmartCampus.Entities
{
    /// <summary>
    /// Ders tipi enum'u
    /// </summary>
    public enum CourseType
    {
        Required,        // Bölüm zorunlu dersi
        Elective,        // Bölüm seçmeli dersi
        GeneralElective  // Genel seçmeli ders (tüm bölümlerden alınabilir)
    }

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
        
        // Ders tipi ve cross-department ayarları
        public CourseType Type { get; set; } = CourseType.Required;
        public bool AllowCrossDepartment { get; set; } = false; // Farklı bölümden alınabilir mi? (Genel seçmeli için true)
        
        // İlişkiler
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
        
        // Navigation Properties
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        public ICollection<CoursePrerequisite> PrerequisiteFor { get; set; } = new List<CoursePrerequisite>();
        public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
        public ICollection<DepartmentCourseRequirement> DepartmentRequirements { get; set; } = new List<DepartmentCourseRequirement>();
    }
}
