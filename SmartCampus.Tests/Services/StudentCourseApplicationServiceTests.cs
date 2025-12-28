using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class StudentCourseApplicationServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly StudentCourseApplicationService _service;

        public StudentCourseApplicationServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _service = new StudentCourseApplicationService(_context);
            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);

            var course = new Course { Id = 1, Name = "Intro to CS", Code = "CS101", Credits = 3, DepartmentId = 1 };
            _context.Courses.Add(course);

            var user = new User { Id = 1, FirstName = "Test", LastName = "Student", Email = "test@test.com", UserName = "test" };
            _context.Users.Add(user);

            var student = new Student { Id = 1, UserId = 1, StudentNumber = "12345", DepartmentId = 1 };
            _context.Students.Add(student);

            var instructor = new User { Id = 2, FirstName = "Prof", LastName = "Test", Email = "prof@test.com", UserName = "prof" };
            _context.Users.Add(instructor);

            var section = new CourseSection { Id = 1, CourseId = 1, SectionNumber = "A", Semester = "Fall", Year = 2024, Capacity = 30, InstructorId = 2 };
            _context.CourseSections.Add(section);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateApplicationAsync_WithValidData_ShouldCreateApplication()
        {
            // Act
            var result = await _service.CreateApplicationAsync(1, 1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CourseId);
            Assert.Equal(1, result.SectionId);
            Assert.Equal(ApplicationStatus.Pending, result.Status);
        }

        [Fact]
        public async Task CreateApplicationAsync_CourseNotFound_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateApplicationAsync(999, 1, 1));
        }

        [Fact]
        public async Task CreateApplicationAsync_SectionNotFound_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateApplicationAsync(1, 999, 1));
        }

        [Fact]
        public async Task CreateApplicationAsync_StudentNotFound_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateApplicationAsync(1, 1, 999));
        }

        [Fact]
        public async Task CreateApplicationAsync_DuplicateApplication_ShouldThrow()
        {
            // Arrange
            await _service.CreateApplicationAsync(1, 1, 1);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateApplicationAsync(1, 1, 1));
        }

        [Fact]
        public async Task GetApplicationsAsync_ShouldReturnApplications()
        {
            // Arrange
            await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.GetApplicationsAsync(null, null, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Data.Count > 0);
        }

        [Fact]
        public async Task GetApplicationsAsync_WithStudentFilter_ShouldFilterByStudent()
        {
            // Arrange
            await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.GetApplicationsAsync(1, null, 1, 10);

            // Assert
            Assert.True(result.Data.All(a => a.StudentId == 1));
        }

        [Fact]
        public async Task GetApplicationByIdAsync_Exists_ShouldReturn()
        {
            // Arrange
            var created = await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.GetApplicationByIdAsync(created.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Id, result.Id);
        }

        [Fact]
        public async Task GetApplicationByIdAsync_NotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetApplicationByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ApproveApplicationAsync_ShouldSetStatusToApproved()
        {
            // Arrange
            var created = await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.ApproveApplicationAsync(created.Id, 100);

            // Assert
            Assert.Equal(ApplicationStatus.Approved, result.Status);
        }

        [Fact]
        public async Task RejectApplicationAsync_ShouldSetStatusToRejected()
        {
            // Arrange
            var created = await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.RejectApplicationAsync(created.Id, 100, "Not eligible");

            // Assert
            Assert.Equal(ApplicationStatus.Rejected, result.Status);
        }

        [Fact]
        public async Task CanStudentApplyAsync_NoExistingApplication_ShouldReturnTrue()
        {
            // Act
            var result = await _service.CanStudentApplyAsync(1, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanStudentApplyAsync_WithExistingApplication_ShouldReturnFalse()
        {
            // Arrange
            await _service.CreateApplicationAsync(1, 1, 1);

            // Act
            var result = await _service.CanStudentApplyAsync(1, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAvailableCoursesForStudentAsync_ShouldReturnCourses()
        {
            // Act
            var result = await _service.GetAvailableCoursesForStudentAsync(1);

            // Assert
            Assert.NotNull(result);
        }
    }
}
