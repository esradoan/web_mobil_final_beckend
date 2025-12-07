using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using Xunit;

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

        [Fact]
        public async Task Register_Should_Return_Ok_On_Success()
        {
            // Arrange
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Pass" };
            var expectedUserDto = new UserDto { Email = "test@example.com", Id = 1 };

            _mockAuthService
                .Setup(s => s.RegisterAsync(registerDto))
                .ReturnsAsync(expectedUserDto);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(expectedUserDto.Email, returnDto.Email);
        }

        [Fact]
        public async Task Login_Should_Return_Ok_With_Token_On_Success()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Pass" };
            var expectedToken = new TokenDto { AccessToken = "abc", RefreshToken = "xyz" };

            _mockAuthService
                .Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnToken = Assert.IsType<TokenDto>(okResult.Value);
            Assert.Equal("abc", returnToken.AccessToken);
        }
    }
}
