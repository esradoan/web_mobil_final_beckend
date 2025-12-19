namespace SmartCampus.Entities
{
    /// <summary>
    /// Yemek rezervasyonunu temsil eder.
    /// Öğrenci bir menü için rezervasyon yapar ve QR kod alır.
    /// </summary>
    public class MealReservation : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Rezervasyonu yapan kullanıcı
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }
        
        /// <summary>
        /// Rezerve edilen menü
        /// </summary>
        public int MenuId { get; set; }
        public MealMenu? Menu { get; set; }
        
        /// <summary>
        /// Yemekhane
        /// </summary>
        public int CafeteriaId { get; set; }
        public Cafeteria? Cafeteria { get; set; }
        
        /// <summary>
        /// Öğün tipi: "lunch" veya "dinner"
        /// </summary>
        public string MealType { get; set; } = "lunch";
        
        /// <summary>
        /// Rezervasyon tarihi
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Ödeme tutarı (burslu öğrenciler için 0)
        /// </summary>
        public decimal Amount { get; set; } = 0;
        
        /// <summary>
        /// Benzersiz QR kod (UUID)
        /// </summary>
        public string QrCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Durum: "reserved", "used", "cancelled"
        /// </summary>
        public string Status { get; set; } = "reserved";
        
        /// <summary>
        /// Kullanım zamanı (yemekhane girişi)
        /// </summary>
        public DateTime? UsedAt { get; set; }
        
        /// <summary>
        /// Burslu mu ücretli mi?
        /// </summary>
        public bool IsScholarship { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
