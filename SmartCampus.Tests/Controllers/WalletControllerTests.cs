using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class WalletControllerTests
    {
        private readonly Mock<IWalletService> _mockService;
        private readonly WalletController _controller;

        public WalletControllerTests()
        {
            _mockService = new Mock<IWalletService>();
            _controller = new WalletController(_mockService.Object);
            SetupUserContext(_controller, userId: 1001);
        }

        private void SetupUserContext(ControllerBase controller, int userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetBalance_ShouldReturnOk_WithBalance()
        {
            // Arrange
            var expected = new WalletDto { Id = 1, Balance = 100m };
            _mockService.Setup(s => s.GetBalanceAsync(1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetBalance();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task TopUp_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var dto = new TopUpRequestDto { Amount = 100m };
            var expected = new TopUpResultDto { Success = true, PaymentUrl = "http://example.com" };
            _mockService.Setup(s => s.CreateTopUpSessionAsync(1001, 100m)).ReturnsAsync(expected);

            // Act
            var result = await _controller.TopUp(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task TopUp_ShouldReturnBadRequest_WhenFailed()
        {
            // Arrange
            var dto = new TopUpRequestDto { Amount = 10m }; // Too low
            var expected = new TopUpResultDto { Success = false, Message = "Minimum amount is 50 TRY" };
            _mockService.Setup(s => s.CreateTopUpSessionAsync(1001, 10m)).ReturnsAsync(expected);

            // Act
            var result = await _controller.TopUp(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CompleteTopUp_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.ProcessTopUpWebhookAsync("ref123", true)).ReturnsAsync(true);

            // Act
            var result = await _controller.CompleteTopUp("ref123");

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ProcessWebhook_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var dto = new PaymentWebhookDto { PaymentReference = "ref123", Success = true };
            _mockService.Setup(s => s.ProcessTopUpWebhookAsync("ref123", true)).ReturnsAsync(true);

            // Act
            var result = await _controller.ProcessWebhook(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetTransactions_ShouldReturnOk_WithTransactions()
        {
            // Arrange
            var expected = new List<TransactionDto> { new TransactionDto { Id = 1, Amount = 50m } };
            _mockService.Setup(s => s.GetTransactionsAsync(1001, 1, 20)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetTransactions(1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task AddBalance_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new AddBalanceDto { UserId = 2001, Amount = 100m, Description = "Bonus" };
            var expected = new WalletDto { Id = 1, Balance = 200m };
            _mockService.Setup(s => s.AddBalanceAsync(2001, 100m, "Bonus")).ReturnsAsync(expected);

            // Act
            var result = await _controller.AddBalance(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }
    }
}
