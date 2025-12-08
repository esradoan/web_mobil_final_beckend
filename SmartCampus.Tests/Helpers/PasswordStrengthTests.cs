#nullable disable
using Xunit;
using SmartCampus.Business.Helpers;

namespace SmartCampus.Tests.Helpers
{
    public class PasswordStrengthTests
    {
        [Fact]
        public void Evaluate_EmptyPassword_ReturnsZeroScore()
        {
            var result = PasswordStrength.Evaluate("");
            Assert.Equal(0, result.Score);
            Assert.Equal("Password is empty", result.Feedback);
        }

        [Fact]
        public void Evaluate_WeakPassword_ReturnsLowScore()
        {
            var result = PasswordStrength.Evaluate("abc");
            Assert.True(result.Score <= 2);
            Assert.Contains("Week", result.Feedback);
        }

        [Fact]
        public void Evaluate_MediumPassword_ReturnsMediumScore()
        {
            var result = PasswordStrength.Evaluate("Password1");
            Assert.True(result.Score >= 2 && result.Score <= 3);
        }

        [Fact]
        public void Evaluate_StrongPassword_ReturnsHighScore()
        {
            var result = PasswordStrength.Evaluate("Password123!");
            Assert.True(result.Score >= 4);
        }
    }
}
