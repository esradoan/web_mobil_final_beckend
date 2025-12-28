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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace SmartCampus.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IEnrollmentService> _mockEnrollmentService;
        private readonly Mock<ITranscriptPdfService> _mockPdfService;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            // Setup InMemory DB
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name per test
                .Options;
            _context = new CampusDbContext(options);

            // Mock Services
            _mockEnrollmentService = new Mock<IEnrollmentService>();
            _mockPdfService = new Mock<ITranscriptPdfService>();

            // Mock UserManager
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Seed initial data
            SeedDatabase();

            _controller = new AdminController(_context, _mockEnrollmentService.Object, _mockPdfService.Object, _mockUserManager.Object);
        }

        private void SeedDatabase()
        {
            var users = new List<User>
            {
                new User { Id = 1, Email = "admin@test.com", FirstName = "Admin", LastName = "User" },
                new User { Id = 2, Email = "student@test.com", FirstName = "Student", LastName = "One" },
                new User { Id = 3, Email = "faculty@test.com", FirstName = "Faculty", LastName = "One" }
            };
            _context.Users.AddRange(users);

            var students = new List<Student>
            {
                new Student { Id = 1, UserId = 2, StudentNumber = "STD001", DepartmentId = 1, IsActive = true }
            };
            _context.Students.AddRange(students);
            
             var departments = new List<Department>
            {
                new Department { Id = 1, Name = "Computer Science" }
            };
            _context.Departments.AddRange(departments);

             var faculties = new List<Faculty>
            {
                new Faculty { Id = 1, UserId = 3, DepartmentId = 1, Title = "Prof", EmployeeNumber = "FAC001" }
            };
            _context.Faculties.AddRange(faculties);

            _context.SaveChanges();
        }

        private void SetupUserContext(int userId, string role = "Admin")
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

        // ==================== Student Tests ====================

        [Fact]
        public async Task GetStudents_Admin_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");

            // Act
            var result = await _controller.GetStudents(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetStudents_Faculty_ReturnsDepartmentStudents()
        {
            // Arrange
            SetupUserContext(3, "Faculty");

            // Act
            var result = await _controller.GetStudents(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        
        [Fact]
        public async Task UpdateStudentStatus_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            var dto = new UpdateStudentStatusDto { IsActive = false };

            // Act
            var result = await _controller.UpdateStudentStatus(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var student = await _context.Students.FindAsync(1);
            Assert.False(student?.IsActive);
        }

        [Fact]
        public async Task GetStudentTranscript_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            _mockEnrollmentService.Setup(s => s.GetTranscriptAsync(2))
                .ReturnsAsync(new TranscriptDto());

            // Act
            var result = await _controller.GetStudentTranscript(1); // Student Id 1 has User Id 2

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetStudentTranscriptPdf_Success_ReturnsFile()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            _mockEnrollmentService.Setup(s => s.GetTranscriptAsync(2))
                .ReturnsAsync(new TranscriptDto());
            _mockPdfService.Setup(s => s.GenerateTranscript(It.IsAny<TranscriptDto>()))
                .Returns(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.GetStudentTranscriptPdf(1);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        // ==================== User Management Tests ====================

        [Fact]
        public async Task CheckUserStatus_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            string email = "student@test.com";
            var user = _context.Users.First(u => u.Email == email);
            
            _mockUserManager.Setup(u => u.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });

            // Act
            var result = await _controller.CheckUserStatus(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DeleteUserByEmail_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            string email = "student@test.com";
            var user = _context.Users.First(u => u.Email == email);

            _mockUserManager.Setup(u => u.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(u => u.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteUserByEmail(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Should also delete student entry
            Assert.Null(await _context.Students.FirstOrDefaultAsync(s => s.UserId == 2));
        }

        [Fact]
        public async Task UpdateUser_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            string email = "student@test.com";
            var user = _context.Users.First(u => u.Email == email);
            
            var dto = new UpdateUserDto 
            { 
                FirstName = "UpdatedFn", 
                LastName = "UpdatedLn" 
            };

            _mockUserManager.Setup(u => u.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(u => u.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);
            _mockUserManager.Setup(u => u.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.UpdateUser(email, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("UpdatedFn", user.FirstName);
        }

        [Fact]
        public async Task ResetUserPassword_Success_ReturnsOk()
        {
            // Arrange
            SetupUserContext(1, "Admin");
            string email = "student@test.com";
            var user = _context.Users.First(u => u.Email == email);
            var dto = new AdminResetPasswordDto { NewPassword = "NewPassword123!" };

            _mockUserManager.Setup(u => u.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(u => u.RemovePasswordAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.AddPasswordAsync(user, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetUserPassword(email, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }
        
        [Fact]
        public async Task FixUserRoles_ReturnsOk_AndUpdatesRoles()
        {
            // Arrange
            var studentUser = new User { Id = 10, Email = "student10@example.com", SecurityStamp = "123" };
            var facultyUser = new User { Id = 11, Email = "faculty11@example.com", SecurityStamp = "456" };
            
            _context.Users.AddRange(studentUser, facultyUser);
            
            var student = new Student { UserId = 10, StudentNumber = "S10" };
            student.User = studentUser; // Link manually for InMemory
            _context.Students.Add(student);
            
            var faculty = new Faculty { UserId = 11, Title = "Prof" };
            faculty.User = facultyUser; // Link manually for InMemory
            _context.Faculties.Add(faculty);
            
            await _context.SaveChangesAsync();

            // Setup UserManager mocks for roles
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>());
            
            // Allow any user for AddToRole
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Student")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Faculty")).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.FixUserRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify at least called once
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), "Student"), Times.AtLeastOnce);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), "Faculty"), Times.AtLeastOnce);
        }
    }
}
