namespace SmartCampus.Entities
{
    /// <summary>
    /// Öğrenci yoklama kaydını temsil eder.
    /// </summary>
    public class AttendanceRecord : IEntity
    {
        public int Id { get; set; }
        
        public int SessionId { get; set; }
        public AttendanceSession? Session { get; set; }
        
        public int StudentId { get; set; }
        public User? Student { get; set; }
        
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        
        public decimal DistanceFromCenter { get; set; }
        
        public bool IsFlagged { get; set; } = false;
        public string? FlagReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
