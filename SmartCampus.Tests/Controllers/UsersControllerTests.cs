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

namespace SmartCampus.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UsersController(_mockUserService.Object);
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
            _mockUserService.Setup(x => x.GetProfileAsync(1)).ReturnsAsync((UserDto?)null);

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
            var userDto = new UserDto { Id = 1, Email = "test@example.com" };

            _mockUserService.Setup(x => x.GetProfileAsync(1)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userDto.Email, returnDto.Email);
        }

        [Fact]
        public async Task UpdateProfile_Should_Call_Service_When_Valid()
        {
            // Arrange
            SetupHttpContext("1");
            var updateDto = new UpdateUserDto { FirstName = "NewName" };
            
            _mockUserService.Setup(x => x.UpdateProfileAsync(1, updateDto)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateProfile(updateDto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(x => x.UpdateProfileAsync(1, updateDto), Times.Once);
        }
        [Fact]
        public async Task GetUsers_ShouldCallServiceWithPagination()
        {
            // Arrange
            SetupHttpContext("1");
            var users = new List<UserDto>();
            _mockUserService.Setup(x => x.GetAllUsersAsync(1, 10)).ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsers(1, 10);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(x => x.GetAllUsersAsync(1, 10), Times.Once);
        }
    }
}
