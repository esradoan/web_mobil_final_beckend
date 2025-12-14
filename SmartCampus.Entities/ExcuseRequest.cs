namespace SmartCampus.Entities
{
    /// <summary>
    /// Mazeret bildirimi/talebini temsil eder.
    /// </summary>
    public class ExcuseRequest : BaseEntity
    {
        public int StudentId { get; set; }
        public User? Student { get; set; }
        
        public int SessionId { get; set; }
        public AttendanceSession? Session { get; set; }
        
        public string Reason { get; set; } = string.Empty;
        public string? DocumentUrl { get; set; }
        
        public string Status { get; set; } = "pending";
        
        public int? ReviewedBy { get; set; }
        public User? Reviewer { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? Notes { get; set; }
    }
}
