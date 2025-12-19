namespace SmartCampus.Entities
{
    /// <summary>
    /// Kullanıcı cüzdanını temsil eder.
    /// Para yükleme, yemek ödemesi gibi işlemler için kullanılır.
    /// </summary>
    public class Wallet : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Cüzdan sahibi
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }
        
        /// <summary>
        /// Mevcut bakiye
        /// </summary>
        public decimal Balance { get; set; } = 0;
        
        /// <summary>
        /// Para birimi (varsayılan TRY)
        /// </summary>
        public string Currency { get; set; } = "TRY";
        
        /// <summary>
        /// Cüzdan aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
