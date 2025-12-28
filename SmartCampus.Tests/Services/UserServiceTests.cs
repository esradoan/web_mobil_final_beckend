using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _mockMapper = new Mock<IMapper>();

            _service = new UserService(_mockUserManager.Object, _mockMapper.Object, _context);
            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);

            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", UserName = "testuser" };
            _context.Users.Add(user);

            var student = new Student { Id = 1, UserId = 1, StudentNumber = "12345", DepartmentId = 1 };
            _context.Students.Add(student);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetProfileAsync_WithExistingUser_ShouldReturnProfile()
        {
            // Arrange
            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Student" });
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Id = 1, FirstName = "Test" });

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetProfileAsync_UserNotFound_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetProfileAsync(999);

            // Assert
            Assert.Null(result);
        }
    }
}
