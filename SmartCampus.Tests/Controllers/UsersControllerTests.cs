#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<CampusDbContext> _mockContext;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            
            // Mock DbContext
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _mockContext = new Mock<CampusDbContext>(options);
            
            // Mock UserManager
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            
            _controller = new UsersController(_mockUserService.Object, _mockContext.Object, _mockUserManager.Object);
        }

        private void SetupHttpContext(string userId)
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

        // GetProfile Tests
        [Fact]
        public async Task GetProfile_ReturnsUnauthorized_WhenUserNotInClaims()
        {
            SetupHttpContext(null);

            var result = await _controller.GetProfile();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetProfile_ReturnsNotFound_WhenUserDoesNotExist()
        {
            SetupHttpContext("1");
            _mockUserService.Setup(x => x.GetProfileAsync(1)).ReturnsAsync((UserDto)null);

            var result = await _controller.GetProfile();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetProfile_ReturnsOk_WhenUserFound()
        {
            SetupHttpContext("1");
            var userDto = new UserDto { Id = 1, Email = "test@example.com" };
            _mockUserService.Setup(x => x.GetProfileAsync(1)).ReturnsAsync(userDto);

            var result = await _controller.GetProfile();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userDto.Email, returnDto.Email);
        }

        // UpdateProfile Tests
        [Fact]
        public async Task UpdateProfile_ReturnsUnauthorized_WhenUserNotInClaims()
        {
            SetupHttpContext(null);

            var result = await _controller.UpdateProfile(new UpdateUserDto());

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1");
            var updateDto = new UpdateUserDto { FirstName = "NewName" };
            _mockUserService.Setup(x => x.UpdateProfileAsync(1, updateDto)).Returns(Task.CompletedTask);

            var result = await _controller.UpdateProfile(updateDto);

            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(x => x.UpdateProfileAsync(1, updateDto), Times.Once);
        }

        // UploadProfilePicture Tests
        [Fact]
        public async Task UploadProfilePicture_ReturnsBadRequest_WhenNoFile()
        {
            SetupHttpContext("1");

            var result = await _controller.UploadProfilePicture(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No file uploaded.", badRequest.Value);
        }

        [Fact]
        public async Task UploadProfilePicture_ReturnsBadRequest_WhenEmptyFile()
        {
            SetupHttpContext("1");
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            var result = await _controller.UploadProfilePicture(mockFile.Object);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No file uploaded.", badRequest.Value);
        }

        [Fact]
        public async Task UploadProfilePicture_ReturnsUnauthorized_WhenUserNotInClaims()
        {
            SetupHttpContext(null);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1000);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var result = await _controller.UploadProfilePicture(mockFile.Object);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UploadProfilePicture_ReturnsBadRequest_WhenNotImage()
        {
            SetupHttpContext("1");
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1000);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var result = await _controller.UploadProfilePicture(mockFile.Object);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Only image files are allowed.", badRequest.Value);
        }

        // GetUsers Tests
        [Fact]
        public async Task GetUsers_ReturnsOk_WithPagination()
        {
            SetupHttpContext("1");
            var users = new List<UserDto>();
            _mockUserService.Setup(x => x.GetAllUsersAsync(1, 10)).ReturnsAsync(users);

            var result = await _controller.GetUsers(1, 10);

            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(x => x.GetAllUsersAsync(1, 10), Times.Once);
        }

        [Fact]
        public async Task GetUsers_UsesDefaultPagination()
        {
            SetupHttpContext("1");
            var users = new List<UserDto>();
            _mockUserService.Setup(x => x.GetAllUsersAsync(1, 10)).ReturnsAsync(users);

            var result = await _controller.GetUsers();

            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(x => x.GetAllUsersAsync(1, 10), Times.Once);
        }

        [Fact]
        public async Task GetUsers_ReturnsCorrectUserList()
        {
            SetupHttpContext("1");
            var users = new List<UserDto>
            {
                new UserDto { Id = 1, Email = "user1@example.com" },
                new UserDto { Id = 2, Email = "user2@example.com" }
            };
            _mockUserService.Setup(x => x.GetAllUsersAsync(1, 10)).ReturnsAsync(users);

            var result = await _controller.GetUsers(1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
            Assert.Equal(2, ((List<UserDto>)returnedUsers).Count);
        }
    }
}
