#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using ApiResetPasswordDto = SmartCampus.API.Controllers.ResetPasswordDto;
using ApiForgotPasswordDto = SmartCampus.API.Controllers.ForgotPasswordDto;

namespace SmartCampus.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        private void SetupHttpContext(string userId = null)
        {
            var user = new ClaimsPrincipal();
            if (userId != null)
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                user.AddIdentity(identity);
            }
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Register Tests
        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Test123!" };
            var userDto = new UserDto { Id = 1, Email = "test@example.com" };
            var registerResponse = new RegisterResponseDto 
            { 
                User = userDto, 
                VerificationUrl = "http://localhost:5173/verify-email",
                VerificationToken = "test-token"
            };
            _mockAuthService.Setup(x => x.RegisterAsync(registerDto)).ReturnsAsync(registerResponse);

            var result = await _controller.Register(registerDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(registerResponse, okResult.Value);
        }

        // Login Tests
        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Test123!" };
            var tokenDto = new TokenDto { AccessToken = "token", RefreshToken = "refresh" };
            _mockAuthService.Setup(x => x.LoginAsync(loginDto)).ReturnsAsync(tokenDto);

            var result = await _controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tokenDto, okResult.Value);
        }

        // Refresh Tests
        [Fact]
        public async Task Refresh_ReturnsOk_WhenTokenValid()
        {
            var refreshDto = new RefreshTokenDto { RefreshToken = "valid-token" };
            var tokenDto = new TokenDto { AccessToken = "new-token", RefreshToken = "new-refresh" };
            _mockAuthService.Setup(x => x.RefreshTokenAsync("valid-token")).ReturnsAsync(tokenDto);

            var result = await _controller.Refresh(refreshDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tokenDto, okResult.Value);
        }

        // Logout Tests
        [Fact]
        public async Task Logout_ReturnsNoContent_WhenUserAuthenticated()
        {
            SetupHttpContext("1");
            _mockAuthService.Setup(x => x.LogoutAsync(1)).Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            Assert.IsType<NoContentResult>(result);
            _mockAuthService.Verify(x => x.LogoutAsync(1), Times.Once);
        }

        [Fact]
        public async Task Logout_ReturnsNoContent_WhenUserNotAuthenticated()
        {
            SetupHttpContext(null);

            var result = await _controller.Logout();

            Assert.IsType<NoContentResult>(result);
            _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<int>()), Times.Never);
        }

        // VerifyEmail Tests
        [Fact]
        public async Task VerifyEmail_ReturnsOk_WhenSuccessful()
        {
            var dto = new VerifyEmailDto { UserId = "1", Token = "token" };
            _mockAuthService.Setup(x => x.VerifyEmailAsync("1", "token")).Returns(Task.CompletedTask);

            var result = await _controller.VerifyEmail(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // ForgotPassword Tests
        [Fact]
        public async Task ForgotPassword_ReturnsOk_Always()
        {
            var dto = new ApiForgotPasswordDto { Email = "test@example.com" };
            _mockAuthService.Setup(x => x.ForgotPasswordAsync("test@example.com")).Returns(Task.CompletedTask);

            var result = await _controller.ForgotPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockAuthService.Verify(x => x.ForgotPasswordAsync("test@example.com"), Times.Once);
        }

        // ResetPassword Tests
        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenPasswordsMatch()
        {
            var dto = new ApiResetPasswordDto
            {
                Email = "test@example.com",
                Token = "token",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };
            _mockAuthService.Setup(x => x.ResetPasswordAsync("test@example.com", "token", "NewPass123!")).Returns(Task.CompletedTask);

            var result = await _controller.ResetPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenPasswordsDontMatch()
        {
            var dto = new ApiResetPasswordDto
            {
                Email = "test@example.com",
                Token = "token",
                NewPassword = "NewPass123!",
                ConfirmPassword = "DifferentPass!"
            };

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // PasswordStrength Tests
        [Fact]
        public void CheckPasswordStrength_ReturnsOk_WithScore()
        {
            var result = _controller.CheckPasswordStrength("StrongPass123!");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
