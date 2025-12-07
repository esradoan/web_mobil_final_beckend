using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.Entities
{
    public class TokenEntitiesTests
    {
        // Tests for PasswordResetToken
        [Fact]
        public void PasswordResetToken_Should_Have_Default_Values()
        {
            var token = new PasswordResetToken();

            Assert.Equal(string.Empty, token.Token);
            Assert.False(token.IsUsed);
            Assert.Equal(0, token.UserId);
            Assert.Null(token.User);
        }

        [Fact]
        public void PasswordResetToken_Should_Set_Properties_Correctly()
        {
            var now = DateTime.UtcNow;
            var token = new PasswordResetToken
            {
                Token = "reset-123",
                ExpiryDate = now.AddHours(1),
                IsUsed = true,
                UserId = 5
            };

            Assert.Equal("reset-123", token.Token);
            Assert.Equal(now.AddHours(1), token.ExpiryDate);
            Assert.True(token.IsUsed);
            Assert.Equal(5, token.UserId);
        }

        [Fact]
        public void PasswordResetToken_Should_Be_Expired_If_Date_Passed()
        {
            var token = new PasswordResetToken
            {
                ExpiryDate = DateTime.UtcNow.AddMinutes(-5) // Expired 5 mins ago
            };

            Assert.True(token.ExpiryDate < DateTime.UtcNow);
        }

        // Tests for RefreshToken
        [Fact]
        public void RefreshToken_Should_Have_Default_Values()
        {
            var token = new RefreshToken();

            Assert.Equal(string.Empty, token.Token);
            Assert.False(token.IsRevoked);
            Assert.Equal(0, token.UserId);
        }

        [Fact]
        public void RefreshToken_Should_Set_Properties_Correctly()
        {
            var now = DateTime.UtcNow;
            var token = new RefreshToken
            {
                Token = "refresh-abc",
                ExpiryDate = now.AddDays(7),
                IsRevoked = true,
                UserId = 10
            };

            Assert.Equal("refresh-abc", token.Token);
            Assert.Equal(now.AddDays(7), token.ExpiryDate);
            Assert.True(token.IsRevoked);
            Assert.Equal(10, token.UserId);
        }

        [Fact]
        public void RefreshToken_Should_Be_Active_If_Not_Expired_And_Not_Revoked()
        {
            var token = new RefreshToken
            {
                ExpiryDate = DateTime.UtcNow.AddMinutes(30),
                IsRevoked = false
            };

            Assert.True(token.ExpiryDate > DateTime.UtcNow);
            Assert.False(token.IsRevoked);
        }
    }
}
