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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Linq;

namespace SmartCampus.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly Mock<IEnrollmentService> _mockEnrollmentService;
        private readonly Mock<ITranscriptPdfService> _mockPdfService;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly CampusDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            
            _mockEnrollmentService = new Mock<IEnrollmentService>();
            _mockPdfService = new Mock<ITranscriptPdfService>();
            
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);
            
            _controller = new AdminController(
                _context,
                _mockEnrollmentService.Object,
                _mockPdfService.Object,
                _mockUserManager.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        private void SetupHttpContext(string userId, string role = "Admin")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // GetStudents Tests
        [Fact]
        public async Task GetStudents_ReturnsOk_WhenAdminCalls()
        {
            SetupHttpContext("1", "Admin");
            
            var department = new Department { Id = 1, Name = "Computer Science", Code = "CS" };
            _context.Departments.Add(department);
            
            var user = new User { Id = 100, Email = "student@test.com", FirstName = "Test", LastName = "Student" };
            _context.Users.Add(user);
            
            var student = new Student { Id = 1, UserId = 100, StudentNumber = "12345", DepartmentId = 1, IsActive = true };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Test basic retrieval
            var result = await _controller.GetStudents();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Test logic: Search
            var resultSearch = await _controller.GetStudents(search: "Test");
            var okResultSearch = Assert.IsType<OkObjectResult>(resultSearch);
            Assert.NotNull(okResultSearch.Value);

            // Test logic: Filter by Department
            var resultDept = await _controller.GetStudents(departmentId: 1);
            Assert.IsType<OkObjectResult>(resultDept);

             // Test logic: Filter by Active
            var resultActive = await _controller.GetStudents(isActive: true);
            Assert.IsType<OkObjectResult>(resultActive);
        }

        [Fact]
        public async Task GetStudents_ReturnsForbid_WhenFacultyHasNoEntry()
        {
            SetupHttpContext("2", "Faculty");
            // No Faculty entry in DB
            
            var result = await _controller.GetStudents();
            
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetStudents_ReturnsOk_WhenFacultyCalls()
        {
            SetupHttpContext("2", "Faculty");
            
            var dept = new Department { Id = 2, Name = "Engineering", Code = "ENG" };
            _context.Departments.Add(dept);
            
            var faculty = new Faculty { Id = 1, UserId = 2, DepartmentId = 2 };
            _context.Faculties.Add(faculty);
            
            var user = new User { Id = 101, Email = "stu2@test.com", FirstName = "S", LastName = "Two" };
            _context.Users.Add(user);
            var student = new Student { Id = 2, UserId = 101, DepartmentId = 2, StudentNumber = "S2" };
            _context.Students.Add(student);
            
            await _context.SaveChangesAsync();

            var result = await _controller.GetStudents();
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // GetStudentTranscript Tests
        [Fact]
        public async Task GetStudentTranscript_ReturnsOk_WhenStudentExists()
        {
            SetupHttpContext("1", "Admin");
            
            // Seed data properly matching navigation properties
            var department = new Department { Id = 1, Name = "CS", Code = "CS101" };
            _context.Departments.Add(department);
            
            var user = new User { Id = 100, Email = "student1@test.com", FirstName = "Test", LastName = "Student" };
            _context.Users.Add(user);
            
            var student = new Student { Id = 1, UserId = 100, StudentNumber = "12345", DepartmentId = 1 };
            _context.Students.Add(student);
            
            await _context.SaveChangesAsync();

            var transcript = new TranscriptDto { StudentName = "Test Student", StudentNumber = "12345" };
            _mockEnrollmentService.Setup(x => x.GetTranscriptAsync(100)).ReturnsAsync(transcript);

            var result = await _controller.GetStudentTranscript(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetStudentTranscript_ReturnsNotFound_WhenStudentNotExists()
        {
            SetupHttpContext("1", "Admin");
            _mockEnrollmentService.Setup(x => x.GetTranscriptAsync(999))
                .ThrowsAsync(new Exception("Student not found"));

            var result = await _controller.GetStudentTranscript(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // GetStudentTranscriptPdf Tests
        [Fact]
        public async Task GetStudentTranscriptPdf_ReturnsFile_WhenStudentExists()
        {
            SetupHttpContext("1", "Admin");
            
            // Seed data properly matching navigation properties
            var department = new Department { Id = 2, Name = "SE", Code = "SE101" };
            _context.Departments.Add(department);
            
            var user = new User { Id = 101, Email = "student2@test.com", FirstName = "Test", LastName = "Student" };
            _context.Users.Add(user);
            
            var student = new Student { Id = 2, UserId = 101, StudentNumber = "54321", DepartmentId = 2 };
            _context.Students.Add(student);
            
            await _context.SaveChangesAsync();

            var transcript = new TranscriptDto { StudentName = "Test Student", StudentNumber = "54321" };
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
            
            _mockEnrollmentService.Setup(x => x.GetTranscriptAsync(101)).ReturnsAsync(transcript);
            _mockPdfService.Setup(x => x.GenerateTranscript(transcript)).Returns(pdfBytes);

            var result = await _controller.GetStudentTranscriptPdf(2);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        [Fact]
        public async Task GetStudentTranscriptPdf_ReturnsNotFound_WhenStudentNotExists()
        {
            SetupHttpContext("1", "Admin");
            var result = await _controller.GetStudentTranscriptPdf(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // CheckUserStatus Tests
        [Fact]
        public async Task CheckUserStatus_ReturnsOk_WhenUserExists()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 10, Email = "user@test.com", FirstName = "Test", EmailConfirmed = true };
            _mockUserManager.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
            _mockUserManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);

            var result = await _controller.CheckUserStatus("user@test.com");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CheckUserStatus_ReturnsNotFound_WhenUserNotExists()
        {
            SetupHttpContext("1", "Admin");
            _mockUserManager.Setup(x => x.FindByEmailAsync("nonexistent@test.com")).ReturnsAsync((User)null);

            var result = await _controller.CheckUserStatus("nonexistent@test.com");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // DeleteUserByEmail Tests
        [Fact]
        public async Task DeleteUserByEmail_ReturnsOk_WhenUserDeleted()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 10, Email = "delete@test.com" };
            _mockUserManager.Setup(x => x.FindByEmailAsync("delete@test.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Add related data to ensure cascade delete logic is covered (though DbContext handles it, Controller checks for it)
            var student = new Student { UserId = 10, Id = 10 };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteUserByEmail("delete@test.com");

            Assert.IsType<OkObjectResult>(result);
            
            // Verify student was removed from context
            Assert.Null(await _context.Students.FindAsync(10));
        }

        [Fact]
        public async Task DeleteUserByEmail_ReturnsNotFound_WhenUserNotExists()
        {
            SetupHttpContext("1", "Admin");
            _mockUserManager.Setup(x => x.FindByEmailAsync("nonexistent@test.com")).ReturnsAsync((User)null);

            var result = await _controller.DeleteUserByEmail("nonexistent@test.com");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ResetUserPassword Tests
        [Fact]
        public async Task ResetUserPassword_ReturnsOk_WhenPasswordReset()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 10, Email = "user@test.com" };
            var dto = new AdminResetPasswordDto { NewPassword = "NewPass123!" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddPasswordAsync(user, dto.NewPassword)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetUserPassword("user@test.com", dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResetUserPassword_ReturnsNotFound_WhenUserNotExists()
        {
            SetupHttpContext("1", "Admin");
            var dto = new AdminResetPasswordDto { NewPassword = "NewPass123!" };
            _mockUserManager.Setup(x => x.FindByEmailAsync("nonexistent@test.com")).ReturnsAsync((User)null);

            var result = await _controller.ResetUserPassword("nonexistent@test.com", dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // --- NEW TESTS FOR COVERAGE ---

        // UpdateStudentStatus Tests
        [Fact]
        public async Task UpdateStudentStatus_ReturnsOk_WhenStudentExists()
        {
            SetupHttpContext("1", "Admin");
            var student = new Student { Id = 5, IsActive = false };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var dto = new UpdateStudentStatusDto { IsActive = true };
            var result = await _controller.UpdateStudentStatus(5, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var updatedStudent = await _context.Students.FindAsync(5);
            Assert.True(updatedStudent.IsActive);
        }

        [Fact]
        public async Task UpdateStudentStatus_ReturnsNotFound_WhenStudentNotExists()
        {
            SetupHttpContext("1", "Admin");
            var dto = new UpdateStudentStatusDto { IsActive = true };
            var result = await _controller.UpdateStudentStatus(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // FixUserRoles Tests
        [Fact]
        public async Task FixUserRoles_ReturnsOk_WhenSuccessful()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 50, Email = "roleless@test.com" };
            _context.Users.Add(user);
            var student = new Student { Id = 50, UserId = 50 }; // Student entry exists
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // No roles
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Student")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.FixUserRoles();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Student"), Times.Once);
        }

        // UpdateUser Tests
        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenUpdatingBasicInfo()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 60, Email = "update@test.com", FirstName = "Old", LastName = "Name" };
            _mockUserManager.Setup(x => x.FindByEmailAsync("update@test.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new UpdateUserDto { FirstName = "New", LastName = "One", Role = null };
            
            var result = await _controller.UpdateUser("update@test.com", dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("New", user.FirstName);
            Assert.Equal("One", user.LastName);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenChangingRole()
        {
            SetupHttpContext("1", "Admin");
            var user = new User { Id = 70, Email = "student@test.com" };
            _mockUserManager.Setup(x => x.FindByEmailAsync("student@test.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Faculty")).ReturnsAsync(IdentityResult.Success);

            var dept = new Department { Id = 2 };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            var dto = new UpdateUserDto { Role = UserRole.Faculty, DepartmentId = 2 };
            
            var result = await _controller.UpdateUser("student@test.com", dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Faculty"), Times.Once);
            
            // Verify faculty entry created
            var facultyEntry = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == 70);
            Assert.NotNull(facultyEntry);
        }
    }
}
