using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using System.Security.Claims;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _mockMapper = new Mock<IMapper>();
            _controller = new UsersController(_mockUserManager.Object, _mockMapper.Object);
        }

        private void SetupHttpContext(string? userId)
        {
            var user = new ClaimsPrincipal();
            if (userId != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                user.AddIdentity(identity);
            }

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetProfile_Should_Return_Unauthorized_When_User_Not_Found_In_Claims()
        {
            // Arrange
            SetupHttpContext(null); // No User ID in claims

            // Act
            var result = await _controller.GetProfile();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetProfile_Should_Return_NotFound_When_User_Does_Not_Exist()
        {
            // Arrange
            SetupHttpContext("1");
            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetProfile_Should_Return_Ok_With_UserDto_When_User_Found()
        {
            // Arrange
            SetupHttpContext("1");
            var user = new User { Id = 1, Email = "test@example.com" };
            var userDto = new UserDto { Id = 1, Email = "test@example.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockMapper.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userDto.Email, returnDto.Email);
        }
    }
}
