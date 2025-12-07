using System;

namespace SmartCampus.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; } = false;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
