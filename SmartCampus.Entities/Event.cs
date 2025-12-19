namespace SmartCampus.Entities
{
    /// <summary>
    /// Kampüs etkinliklerini temsil eder.
    /// Konferans, workshop, sosyal etkinlik, spor aktivitesi gibi.
    /// </summary>
    public class Event : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Etkinlik başlığı
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Etkinlik açıklaması
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Kategori: "conference", "workshop", "social", "sports", "cultural"
        /// </summary>
        public string Category { get; set; } = "social";
        
        /// <summary>
        /// Etkinlik tarihi
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Başlangıç saati
        /// </summary>
        public TimeSpan StartTime { get; set; }
        
        /// <summary>
        /// Bitiş saati
        /// </summary>
        public TimeSpan EndTime { get; set; }
        
        /// <summary>
        /// Konum
        /// </summary>
        public string Location { get; set; } = string.Empty;
        
        /// <summary>
        /// Maksimum katılımcı sayısı
        /// </summary>
        public int Capacity { get; set; }
        
        /// <summary>
        /// Kayıtlı katılımcı sayısı
        /// </summary>
        public int RegisteredCount { get; set; } = 0;
        
        /// <summary>
        /// Son kayıt tarihi
        /// </summary>
        public DateTime RegistrationDeadline { get; set; }
        
        /// <summary>
        /// Ücretli etkinlik mi?
        /// </summary>
        public bool IsPaid { get; set; } = false;
        
        /// <summary>
        /// Ücret (ücretli ise)
        /// </summary>
        public decimal Price { get; set; } = 0;
        
        /// <summary>
        /// Durum: "draft", "published", "cancelled", "completed"
        /// </summary>
        public string Status { get; set; } = "draft";
        
        /// <summary>
        /// Etkinlik görseli URL
        /// </summary>
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Organizatör ID
        /// </summary>
        public int OrganizerId { get; set; }
        public User? Organizer { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }
}
