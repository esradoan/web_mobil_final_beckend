namespace SmartCampus.Entities
{
    /// <summary>
    /// Kampüsteki yemekhaneleri temsil eder.
    /// Her yemekhanenin menüleri ve rezervasyonları vardır.
    /// </summary>
    public class Cafeteria : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Yemekhanenin adı (ör: "Merkez Yemekhane", "Mühendislik Kafeteryası")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Konum bilgisi (ör: "Ana Kampüs, A Blok")
        /// </summary>
        public string Location { get; set; } = string.Empty;
        
        /// <summary>
        /// Maksimum kapasite (kişi sayısı)
        /// </summary>
        public int Capacity { get; set; }
        
        /// <summary>
        /// Aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<MealMenu> Menus { get; set; } = new List<MealMenu>();
        public ICollection<MealReservation> Reservations { get; set; } = new List<MealReservation>();
    }
}
