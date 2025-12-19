using Xunit;
using SmartCampus.API.Controllers;

namespace SmartCampus.Tests.DTOs
{
    public class ControllerDtoTests
    {
        [Fact]
        public void AddBalanceDto_Properties_SetGetCorrectly()
        {
            // Arrange
            var dto = new SmartCampus.API.Controllers.AddBalanceDto();
            var userId = 123;
            var amount = 150.50m;
            var description = "Test Balance";

            // Act
            dto.UserId = userId;
            dto.Amount = amount;
            dto.Description = description;

            // Assert
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(amount, dto.Amount);
            Assert.Equal(description, dto.Description);
        }

        [Fact]
        public void ApprovalDto_Properties_SetGetCorrectly()
        {
            // Arrange
            var dto = new SmartCampus.API.Controllers.ApprovalDto();
            var notes = "Approved via test";

            // Act
            dto.Notes = notes;

            // Assert
            Assert.Equal(notes, dto.Notes);
        }

        [Fact]
        public void CheckInDto_Properties_SetGetCorrectly()
        {
            // Arrange
            var dto = new SmartCampus.API.Controllers.CheckInDto();
            var qrCode = "UUID-1234-5678";

            // Act
            dto.QrCode = qrCode;

            // Assert
            Assert.Equal(qrCode, dto.QrCode);
        }
    }
}
