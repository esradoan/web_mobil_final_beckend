namespace SmartCampus.Entities
{
    /// <summary>
    /// Ders ön koşul ilişkisini temsil eder (Many-to-Many self-referencing).
    /// </summary>
    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        
        public int PrerequisiteCourseId { get; set; }
        public Course? PrerequisiteCourse { get; set; }
    }
}
