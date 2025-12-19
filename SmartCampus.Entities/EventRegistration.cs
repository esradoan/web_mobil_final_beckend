namespace SmartCampus.Entities
{
    /// <summary>
    /// Etkinlik kayıtlarını temsil eder.
    /// Her kayıt için QR kod üretilir ve giriş takibi yapılır.
    /// </summary>
    public class EventRegistration : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Etkinlik
        /// </summary>
        public int EventId { get; set; }
        public Event? Event { get; set; }
        
        /// <summary>
        /// Kayıt yapan kullanıcı
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }
        
        /// <summary>
        /// Kayıt tarihi
        /// </summary>
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Benzersiz QR kod
        /// </summary>
        public string QrCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Check-in yapıldı mı?
        /// </summary>
        public bool CheckedIn { get; set; } = false;
        
        /// <summary>
        /// Check-in zamanı
        /// </summary>
        public DateTime? CheckedInAt { get; set; }
        
        /// <summary>
        /// Özel alanlar (ek form verileri - JSON)
        /// Örnek: {"dietary": "vegetarian", "tshirt_size": "M"}
        /// </summary>
        public string? CustomFieldsJson { get; set; }
        
        /// <summary>
        /// Durum: "registered", "cancelled", "waitlist"
        /// </summary>
        public string Status { get; set; } = "registered";
        
        /// <summary>
        /// Ödeme yapıldı mı? (ücretli etkinlikler için)
        /// </summary>
        public bool IsPaid { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
