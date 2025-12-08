#nullable disable
using Xunit;
using SmartCampus.Entities;
using System;

namespace SmartCampus.Tests.Entities
{
    public class UserEntityTests
    {
        [Fact]
        public void User_DefaultValues_AreCorrect()
        {
            var user = new User();

            Assert.Equal(string.Empty, user.FirstName);
            Assert.Equal(string.Empty, user.LastName);
            Assert.False(user.IsDeleted);
            Assert.Null(user.UpdatedAt);
            Assert.Null(user.EmailVerificationToken);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            Assert.Null(user.ProfilePictureUrl);
        }

        [Fact]
        public void User_CreatedAt_IsSetToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var user = new User();
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.True(user.CreatedAt >= before && user.CreatedAt <= after);
        }

        [Fact]
        public void User_CanSetFirstName()
        {
            var user = new User { FirstName = "John" };
            Assert.Equal("John", user.FirstName);
        }

        [Fact]
        public void User_CanSetLastName()
        {
            var user = new User { LastName = "Doe" };
            Assert.Equal("Doe", user.LastName);
        }

        [Fact]
        public void User_CanSetIsDeleted()
        {
            var user = new User { IsDeleted = true };
            Assert.True(user.IsDeleted);
        }

        [Fact]
        public void User_CanSetUpdatedAt()
        {
            var updateTime = DateTime.UtcNow;
            var user = new User { UpdatedAt = updateTime };
            Assert.Equal(updateTime, user.UpdatedAt);
        }

        [Fact]
        public void User_CanSetEmailVerificationToken()
        {
            var user = new User { EmailVerificationToken = "token123" };
            Assert.Equal("token123", user.EmailVerificationToken);
        }

        [Fact]
        public void User_CanSetRefreshToken()
        {
            var user = new User { RefreshToken = "refresh-token" };
            Assert.Equal("refresh-token", user.RefreshToken);
        }

        [Fact]
        public void User_CanSetRefreshTokenExpiryTime()
        {
            var expiry = DateTime.UtcNow.AddDays(7);
            var user = new User { RefreshTokenExpiryTime = expiry };
            Assert.Equal(expiry, user.RefreshTokenExpiryTime);
        }

        [Fact]
        public void User_CanSetProfilePictureUrl()
        {
            var user = new User { ProfilePictureUrl = "/uploads/pic.jpg" };
            Assert.Equal("/uploads/pic.jpg", user.ProfilePictureUrl);
        }

        [Fact]
        public void User_ImplementsIAuditEntity()
        {
            var user = new User();
            Assert.IsAssignableFrom<IAuditEntity>(user);
        }

        [Fact]
        public void User_InheritsFromIdentityUser()
        {
            var user = new User();
            Assert.IsAssignableFrom<Microsoft.AspNetCore.Identity.IdentityUser<int>>(user);
        }

        [Fact]
        public void User_CanSetId()
        {
            var user = new User { Id = 123 };
            Assert.Equal(123, user.Id);
        }

        [Fact]
        public void User_CanSetEmail()
        {
            var user = new User { Email = "test@example.com" };
            Assert.Equal("test@example.com", user.Email);
        }

        [Fact]
        public void User_CanSetUserName()
        {
            var user = new User { UserName = "testuser" };
            Assert.Equal("testuser", user.UserName);
        }

        [Fact]
        public void User_CanSetPhoneNumber()
        {
            var user = new User { PhoneNumber = "+1234567890" };
            Assert.Equal("+1234567890", user.PhoneNumber);
        }
    }
}
