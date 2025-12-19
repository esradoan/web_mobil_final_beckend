namespace SmartCampus.Entities
{
    /// <summary>
    /// Cüzdan işlemlerini temsil eder.
    /// Para yükleme, yemek ödemesi gibi tüm finansal hareketler burada kaydedilir.
    /// </summary>
    public class Transaction : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// İlişkili cüzdan
        /// </summary>
        public int WalletId { get; set; }
        public Wallet? Wallet { get; set; }
        
        /// <summary>
        /// İşlem tipi: "credit" (para yükleme) veya "debit" (harcama)
        /// </summary>
        public string Type { get; set; } = "credit";
        
        /// <summary>
        /// İşlem tutarı (pozitif değer)
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// İşlem sonrası bakiye
        /// </summary>
        public decimal BalanceAfter { get; set; }
        
        /// <summary>
        /// Referans tipi: "meal", "event", "topup", "refund"
        /// </summary>
        public string ReferenceType { get; set; } = string.Empty;
        
        /// <summary>
        /// Referans ID (ör: MealReservation ID, Event ID)
        /// </summary>
        public int? ReferenceId { get; set; }
        
        /// <summary>
        /// İşlem açıklaması
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Ödeme gateway'i referans kodu (Stripe/PayTR)
        /// </summary>
        public string? PaymentReference { get; set; }
        
        /// <summary>
        /// İşlem durumu: "pending", "completed", "failed", "refunded"
        /// </summary>
        public string Status { get; set; } = "completed";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
