using Xunit;
using SmartCampus.API.Controllers;

namespace SmartCampus.Tests.DTOs
{
    public class AuthDtosTests
    {
        [Fact]
        public void ForgotPasswordDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new ForgotPasswordDto();
            var email = "test@example.com";

            // Act
            dto.Email = email;

            // Assert
            Assert.Equal(email, dto.Email);
        }

        [Fact]
        public void RefreshTokenDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new RefreshTokenDto();
            var token = "some-refresh-token";

            // Act
            dto.RefreshToken = token;

            // Assert
            Assert.Equal(token, dto.RefreshToken);
        }

        [Fact]
        public void ResetPasswordDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new ResetPasswordDto();
            var email = "test@example.com";
            var token = "reset-token";
            var newPassword = "NewPassword123!";
            var confirmPassword = "NewPassword123!";

            // Act
            dto.Email = email;
            dto.Token = token;
            dto.NewPassword = newPassword;
            dto.ConfirmPassword = confirmPassword;

            // Assert
            Assert.Equal(email, dto.Email);
            Assert.Equal(token, dto.Token);
            Assert.Equal(newPassword, dto.NewPassword);
            Assert.Equal(confirmPassword, dto.ConfirmPassword);
        }
        [Fact]
        public void VerifyEmailDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new VerifyEmailDto();
            var userId = "123";
            var token = "verify-token";

            // Act
            dto.UserId = userId;
            dto.Token = token;

            // Assert
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(token, dto.Token);
        }

        [Fact]
        public void Business_ForgotPasswordDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new SmartCampus.Business.DTOs.ForgotPasswordDto();
            var email = "business@example.com";

            // Act
            dto.Email = email;

            // Assert
            Assert.Equal(email, dto.Email);
        }

        [Fact]
        public void Business_ResetPasswordDto_SetAndGetProperties_ReturnsCorrectValues()
        {
            // Arrange
            var dto = new SmartCampus.Business.DTOs.ResetPasswordDto();
            var email = "business@example.com";
            var token = "reset-token";
            var newPassword = "NewPassword123!";
            var confirmPassword = "NewPassword123!";

            // Act
            dto.Email = email;
            dto.Token = token;
            dto.NewPassword = newPassword;
            dto.ConfirmPassword = confirmPassword;

            // Assert
            Assert.Equal(email, dto.Email);
            Assert.Equal(token, dto.Token);
            Assert.Equal(newPassword, dto.NewPassword);
            Assert.Equal(confirmPassword, dto.ConfirmPassword);
        }
    }
}
