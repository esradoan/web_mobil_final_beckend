using Moq;
using Xunit;
using SmartCampus.Business.Services;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartCampus.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup Config for JWT
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(x => x["Secret"]).Returns("SuperSecretKeyForSmartCampusProject_MustBeVeryLong_AtLeast32Chars");
            mockJwtSection.Setup(x => x["Issuer"]).Returns("SmartCampusAPI");
            mockJwtSection.Setup(x => x["Audience"]).Returns("SmartCampusClient");
            mockJwtSection.Setup(x => x["AccessTokenExpirationMinutes"]).Returns("15");

            _mockConfiguration.Setup(c => c.GetSection("JwtSettings")).Returns(mockJwtSection.Object);

            _authService = new AuthService(_mockUserManager.Object, _mockMapper.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnUserDto_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Password123!", FirstName = "Test", LastName = "User" };
            var user = new User { Email = "test@example.com", UserName = "test@example.com" };
            var userDto = new UserDto { Email = "test@example.com", FirstName = "Test" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((User)null); // Correct: returns Task<User> which is null
            _mockMapper.Setup(m => m.Map<User>(registerDto)).Returns(user);
            _mockUserManager.Setup(x => x.CreateAsync(user, registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.Email, result.Email);
        }
    }
}
