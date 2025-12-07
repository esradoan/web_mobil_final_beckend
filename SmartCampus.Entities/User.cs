using System;
using Microsoft.AspNetCore.Identity;

namespace SmartCampus.Entities
{
    // Removing UserRole enum as we will use IdentityRole
    
    public class User : IdentityUser<int>, IAuditEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // IdentityUser already has: Id, UserName, Email, PasswordHash, PhoneNumber, etc.
        
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? EmailVerificationToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
