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
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class UsersControllerTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);

            _mockUserService = new Mock<IUserService>();
            
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _controller = new UsersController(_mockUserService.Object, _context, _mockUserManager.Object);
            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);

            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", UserName = "test" };
            _context.Users.Add(user);

            var faculty = new Faculty { Id = 1, UserId = 1, DepartmentId = 1, EmployeeNumber = "E001", Title = "Professor" };
            _context.Faculties.Add(faculty);

            _context.SaveChanges();
        }

        private void SetupUserContext(int userId, string role = "Student")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetProfile_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);
            _mockUserService.Setup(s => s.GetProfileAsync(1))
                .ReturnsAsync(new UserDto { Id = 1 });

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetProfile_NoUserId_ReturnsUnauthorized()
        {
            // Arrange - empty claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetProfile();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(999);
            _mockUserService.Setup(s => s.GetProfileAsync(999))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1);

            // Act
            var result = await _controller.UpdateProfile(new UpdateUserDto());

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_NoUserId_ReturnsUnauthorized()
        {
            // Arrange - empty claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.UpdateProfile(new UpdateUserDto());

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetUsers_Admin_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");

            // Act
            var result = await _controller.GetUsers(1, 10);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFaculty_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");

            // Act
            var result = await _controller.GetFaculty();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UploadProfilePicture_NoFile_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1);

            // Act
            var result = await _controller.UploadProfilePicture(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadProfilePicture_InvalidFileType_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(1);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            // Act
            var result = await _controller.UploadProfilePicture(mockFile.Object);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadProfilePicture_NoUserId_ReturnsUnauthorized()
        {
            // Arrange - empty claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            // Act
            var result = await _controller.UploadProfilePicture(mockFile.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
    }
}
