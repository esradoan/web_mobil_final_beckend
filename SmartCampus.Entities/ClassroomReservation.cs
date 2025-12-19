namespace SmartCampus.Entities
{
    /// <summary>
    /// Derslik rezervasyonunu temsil eder.
    /// Kullanıcılar derslik rezerve edebilir, admin onaylar.
    /// </summary>
    public class ClassroomReservation : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Rezerve edilen derslik
        /// </summary>
        public int ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }
        
        /// <summary>
        /// Rezervasyonu yapan kullanıcı
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }
        
        /// <summary>
        /// Rezervasyon tarihi
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
        /// Kullanım amacı
        /// </summary>
        public string Purpose { get; set; } = string.Empty;
        
        /// <summary>
        /// Durum: "pending", "approved", "rejected", "cancelled"
        /// </summary>
        public string Status { get; set; } = "pending";
        
        /// <summary>
        /// Onaylayan admin
        /// </summary>
        public int? ApprovedBy { get; set; }
        public User? Approver { get; set; }
        
        /// <summary>
        /// Onay/red tarihi
        /// </summary>
        public DateTime? ReviewedAt { get; set; }
        
        /// <summary>
        /// Notlar (onay/red nedeni)
        /// </summary>
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
