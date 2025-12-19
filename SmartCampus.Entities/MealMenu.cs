namespace SmartCampus.Entities
{
    /// <summary>
    /// Günlük yemek menüsünü temsil eder.
    /// Her menü bir tarih, öğün tipi ve yemek listesi içerir.
    /// </summary>
    public class MealMenu : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Hangi yemekhanenin menüsü
        /// </summary>
        public int CafeteriaId { get; set; }
        public Cafeteria? Cafeteria { get; set; }
        
        /// <summary>
        /// Menü tarihi
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Öğün tipi: "lunch" veya "dinner"
        /// </summary>
        public string MealType { get; set; } = "lunch";
        
        /// <summary>
        /// Yemek listesi (JSON)
        /// Örnek: ["Mercimek Çorbası", "Tavuk Sote", "Pilav", "Salata"]
        /// </summary>
        public string ItemsJson { get; set; } = "[]";
        
        /// <summary>
        /// Besin değerleri (JSON)
        /// Örnek: {"calories": 850, "protein": 35, "carbs": 90, "fat": 25}
        /// </summary>
        public string NutritionJson { get; set; } = "{}";
        
        /// <summary>
        /// Menü yayınlandı mı?
        /// </summary>
        public bool IsPublished { get; set; } = false;
        
        /// <summary>
        /// Vejetaryen/Vegan seçeneği var mı?
        /// </summary>
        public bool HasVegetarianOption { get; set; } = false;
        
        /// <summary>
        /// Fiyat (ücretli öğrenciler için)
        /// </summary>
        public decimal Price { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<MealReservation> Reservations { get; set; } = new List<MealReservation>();
    }
}
