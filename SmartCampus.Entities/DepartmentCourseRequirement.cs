namespace SmartCampus.Entities
{
    /// <summary>
    /// Bölüm ders gereksinimlerini temsil eder.
    /// Bir bölümün hangi dersleri zorunlu olduğunu tanımlar.
    /// </summary>
    public class DepartmentCourseRequirement : BaseEntity
    {
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        
        /// <summary>
        /// Bu ders bölüm için zorunlu mu?
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Minimum geçme notu (opsiyonel, null ise standart geçme notu kullanılır)
        /// </summary>
        public int? MinimumGrade { get; set; }
        
        /// <summary>
        /// Hangi sınıfta alınması gerektiği (opsiyonel, örn: 1, 2, 3, 4)
        /// </summary>
        public int? RecommendedYear { get; set; }
    }
}

