#nullable disable
using Moq;
using Xunit;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using SmartCampus.DataAccess;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SmartCampus.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<CampusDbContext> _mockContext;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _mockMapper = new Mock<IMapper>();
            _mockContext = new Mock<CampusDbContext>();
            _userService = new UserService(_mockUserManager.Object, _mockMapper.Object, _mockContext.Object);
        }

        // GetProfileAsync Tests
        [Fact]
        public async Task GetProfileAsync_ReturnsNull_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((User)null);

            var result = await _userService.GetProfileAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsUserDto_WhenUserFound()
        {
            var user = new User { Id = 1, Email = "test@example.com" };
            var userDto = new UserDto { Id = 1, Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockMapper.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            var result = await _userService.GetProfileAsync(1);

            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetProfileAsync_CallsMapperWithCorrectUser()
        {
            var user = new User { Id = 5, Email = "mapper@test.com" };
            var userDto = new UserDto { Id = 5, Email = "mapper@test.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync("5")).ReturnsAsync(user);
            _mockMapper.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            await _userService.GetProfileAsync(5);

            _mockMapper.Verify(x => x.Map<UserDto>(user), Times.Once);
        }

        // UpdateProfileAsync Tests
        [Fact]
        public async Task UpdateProfileAsync_ThrowsException_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<System.Exception>(() => 
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

            var ex = await Assert.ThrowsAsync<System.Exception>(() => 
                _userService.UpdateProfileAsync(1, updateDto));

            Assert.Contains("Update failed", ex.Message);
        }

        // UpdateProfilePictureAsync Tests
        [Fact]
        public async Task UpdateProfilePictureAsync_ThrowsException_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<System.Exception>(() => 
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

            var ex = await Assert.ThrowsAsync<System.Exception>(() => 
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
