namespace SmartCampus.Entities
{
    /// <summary>
    /// Ders programı kaydını temsil eder.
    /// Bir section'ın hangi gün, saat ve sınıfta olduğunu tanımlar.
    /// </summary>
    public class Schedule : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Ders section'ı
        /// </summary>
        public int SectionId { get; set; }
        public CourseSection? Section { get; set; }
        
        /// <summary>
        /// Haftanın günü: 0=Pazar, 1=Pazartesi, ... 6=Cumartesi
        /// </summary>
        public int DayOfWeek { get; set; }
        
        /// <summary>
        /// Başlangıç saati
        /// </summary>
        public TimeSpan StartTime { get; set; }
        
        /// <summary>
        /// Bitiş saati
        /// </summary>
        public TimeSpan EndTime { get; set; }
        
        /// <summary>
        /// Derslik
        /// </summary>
        public int ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }
        
        /// <summary>
        /// Akademik dönem: "fall", "spring", "summer"
        /// </summary>
        public string Semester { get; set; } = "fall";
        
        /// <summary>
        /// Akademik yıl
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// Aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
