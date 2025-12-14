namespace SmartCampus.Entities
{
    /// <summary>
    /// Sınıf/Derslik bilgilerini temsil eder.
    /// GPS koordinatları yoklama sistemi için kullanılır.
    /// </summary>
    public class Classroom : BaseEntity
    {
        public string Building { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        
        // GPS Koordinatları (Yoklama için)
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        
        // Sınıf özellikleri (projeksiyon, tahta, vb.) - JSON formatında
        public string? FeaturesJson { get; set; }
    }
}
