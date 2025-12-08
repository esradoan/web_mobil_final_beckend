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
using System;

namespace SmartCampus.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<SmartCampus.DataAccess.Repositories.IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<SmartCampus.DataAccess.Repositories.IGenericRepository<RefreshToken>> _mockRefreshTokenRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUnitOfWork = new Mock<SmartCampus.DataAccess.Repositories.IUnitOfWork>();
            _mockRefreshTokenRepo = new Mock<SmartCampus.DataAccess.Repositories.IGenericRepository<RefreshToken>>();
            _mockEmailService = new Mock<IEmailService>();

            _mockUnitOfWork.Setup(u => u.Repository<RefreshToken>()).Returns(_mockRefreshTokenRepo.Object);
            _mockRefreshTokenRepo.Setup(x => x.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).Returns(Task.FromResult(1));

            // Setup Config for JWT
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(x => x["Secret"]).Returns("SuperSecretKeyForSmartCampusProject_MustBeVeryLong_AtLeast32Chars");
            mockJwtSection.Setup(x => x["Issuer"]).Returns("SmartCampusAPI");
            mockJwtSection.Setup(x => x["Audience"]).Returns("SmartCampusClient");
            mockJwtSection.Setup(x => x["AccessTokenExpirationMinutes"]).Returns("15");

            _mockConfiguration.Setup(c => c.GetSection("JwtSettings")).Returns(mockJwtSection.Object);

            _authService = new AuthService(_mockUserManager.Object, _mockMapper.Object, _mockConfiguration.Object, _mockUnitOfWork.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnUserDto_WhenRegistrationIsSuccessful_Student()
        {
            // Arrange
            var registerDto = new RegisterDto 
            { 
                Email = "test@example.com", 
                Password = "Password123!", 
                FirstName = "Test", 
                LastName = "User",
                Role = UserRole.Student,
                StudentNumber = "12345",
                DepartmentId = 1
            };
            var user = new User { Id = 1, Email = "test@example.com", UserName = "test@example.com" };
            var userDto = new UserDto { Email = "test@example.com", FirstName = "Test" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((User)null);
            _mockMapper.Setup(m => m.Map<User>(registerDto)).Returns(user);
            _mockUserManager.Setup(x => x.CreateAsync(user, registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);
            
            var mockStudentRepo = new Mock<SmartCampus.DataAccess.Repositories.IGenericRepository<Student>>();
            mockStudentRepo.Setup(r => r.AddAsync(It.IsAny<Student>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.Repository<Student>()).Returns(mockStudentRepo.Object);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.Email, result.Email);
            mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenStudentMissingDetails()
        {
             // Arrange
            var registerDto = new RegisterDto 
            { 
                Email = "test@example.com", 
                Password = "Password123!", 
                Role = UserRole.Student 
                // Missing StudentNumber and DepartmentId
            };
            var user = new User { Id = 1, Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((User)null);
            _mockMapper.Setup(m => m.Map<User>(registerDto)).Returns(user);
            // Even if CreateAsync succeeds, it should fail before completion
            _mockUserManager.Setup(x => x.CreateAsync(user, registerDto.Password)).ReturnsAsync(IdentityResult.Success);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDto));
            Assert.Equal("Student Number and Department are required for Students.", exception.Message);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenUserAlreadyExists()
        {
            // Arrange
            var registerDto = new RegisterDto { Email = "existing@example.com", Password = "Password123!" };
            var existingUser = new User { Email = "existing@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDto));
            Assert.Contains("User with this email already exists", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = 1, Email = "test@example.com", UserName = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "unknown@example.com", Password = "Password123!" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
            Assert.Equal("Invalid credentials", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowException_WhenPasswordIsInvalid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "WrongPassword!" };
            var user = new User { Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
            Assert.Equal("Invalid credentials", exception.Message);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldSucceed_WhenTokenIsValid()
        {
            // Arrange
            var userId = "1";
            var token = "valid-token";
            var user = new User { Id = 1, Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.VerifyEmailAsync(userId, token);

            // Assert
            _mockUserManager.Verify(x => x.ConfirmEmailAsync(user, token), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var userId = "1";
            var token = "any-token";
            
            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.VerifyEmailAsync(userId, token));
            Assert.Equal("User not found", exception.Message);
        }
    }
}
