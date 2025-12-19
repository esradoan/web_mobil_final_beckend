#nullable disable
using Moq;
using Xunit;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using SmartCampus.DataAccess;
using System.Threading.Tasks;
using System;

namespace SmartCampus.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CampusDbContext _context;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _mockMapper = new Mock<IMapper>();
            
            // Use InMemory database instead of mocking DbContext
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            
            _userService = new UserService(_mockUserManager.Object, _mockMapper.Object, _context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        // GetProfileAsync Tests
        [Fact]
        public async Task GetProfileAsync_ReturnsNull_WhenUserNotFound()
        {
            // User not in InMemory DB
            var result = await _userService.GetProfileAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsUserDto_WhenUserFound()
        {
            // Add user to InMemory DB (GetProfileAsync uses _context.Users, not UserManager)
            var user = new User { Id = 100, Email = "test@example.com", FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Add Student entry for role detection
            var student = new Student { Id = 1, UserId = 100, StudentNumber = "12345", DepartmentId = 1 };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            
            var userDto = new UserDto { Id = 100, Email = "test@example.com" };
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>())).Returns(userDto);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new System.Collections.Generic.List<string> { "Student" });

            var result = await _userService.GetProfileAsync(100);

            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetProfileAsync_CallsMapperWithCorrectUser()
        {
            // Add user to InMemory DB
            var user = new User { Id = 200, Email = "mapper@test.com", FirstName = "Mapper", LastName = "Test" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Add Student entry for role detection
            var student = new Student { Id = 2, UserId = 200, StudentNumber = "54321", DepartmentId = 1 };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            
            var userDto = new UserDto { Id = 200, Email = "mapper@test.com" };
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>())).Returns(userDto);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new System.Collections.Generic.List<string> { "Student" });

            await _userService.GetProfileAsync(200);

            _mockMapper.Verify(x => x.Map<UserDto>(It.IsAny<User>()), Times.Once);
        }

        // UpdateProfileAsync Tests
        [Fact]
        public async Task UpdateProfileAsync_ThrowsException_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<Exception>(() => 
                _userService.UpdateProfileAsync(999, new UpdateUserDto()));
        }

        [Fact]
        public async Task UpdateProfileAsync_UpdatesUserFields_WhenUserFound()
        {
            var user = new User { Id = 1, FirstName = "Old", LastName = "Name" };
            var updateDto = new UpdateUserDto { FirstName = "New", LastName = "Name", PhoneNumber = "123456" };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _userService.UpdateProfileAsync(1, updateDto);

            Assert.Equal("New", user.FirstName);
            Assert.Equal("Name", user.LastName);
            Assert.Equal("123456", user.PhoneNumber);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_ThrowsException_WhenUpdateFails()
        {
            var user = new User { Id = 1 };
            var updateDto = new UpdateUserDto { FirstName = "Test" };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            var ex = await Assert.ThrowsAsync<Exception>(() => 
                _userService.UpdateProfileAsync(1, updateDto));

            Assert.Contains("Update failed", ex.Message);
        }

        // UpdateProfilePictureAsync Tests
        [Fact]
        public async Task UpdateProfilePictureAsync_ThrowsException_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<Exception>(() => 
                _userService.UpdateProfilePictureAsync(999, "/some/url.jpg"));
        }

        [Fact]
        public async Task UpdateProfilePictureAsync_UpdatesPictureUrl_WhenUserFound()
        {
            var user = new User { Id = 1, ProfilePictureUrl = null };
            var newUrl = "/uploads/newpic.jpg";

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _userService.UpdateProfilePictureAsync(1, newUrl);

            Assert.Equal(newUrl, user.ProfilePictureUrl);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateProfilePictureAsync_ThrowsException_WhenUpdateFails()
        {
            var user = new User { Id = 1 };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Picture update failed" }));

            var ex = await Assert.ThrowsAsync<Exception>(() => 
                _userService.UpdateProfilePictureAsync(1, "/url.jpg"));

            Assert.Contains("Picture update failed", ex.Message);
        }

        [Fact]
        public async Task UpdateProfilePictureAsync_SetsUpdatedAt()
        {
            var user = new User { Id = 1, UpdatedAt = null };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _userService.UpdateProfilePictureAsync(1, "/url.jpg");

            Assert.NotNull(user.UpdatedAt);
        }
    }
}
