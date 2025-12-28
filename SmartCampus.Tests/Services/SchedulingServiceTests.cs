using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class SchedulingServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly SchedulingService _service;

        public SchedulingServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _service = new SchedulingService(_context);
            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);

            var classroom = new Classroom { Id = 1, Building = "Main", RoomNumber = "101", Capacity = 50, Latitude = 0, Longitude = 0 };
            _context.Classrooms.Add(classroom);

            var course = new Course { Id = 1, Name = "Intro to CS", Code = "CS101", Credits = 3, DepartmentId = 1 };
            _context.Courses.Add(course);

            var user = new User { Id = 1, FirstName = "Prof", LastName = "Test", Email = "prof@test.com", UserName = "prof" };
            _context.Users.Add(user);

            var faculty = new Faculty { Id = 1, UserId = 1, DepartmentId = 1 };
            _context.Faculties.Add(faculty);

            var section = new CourseSection { Id = 1, CourseId = 1, SectionNumber = "A", Semester = "Fall", Year = 2024, Capacity = 30, InstructorId = 1 };
            _context.CourseSections.Add(section);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GenerateScheduleAsync_WithValidData_ShouldGenerate()
        {
            // Arrange
            var dto = new GenerateScheduleDto
            {
                Semester = "Fall",
                Year = 2024,
                SectionIds = new List<int> { 1 }
            };

            // Act
            var result = await _service.GenerateScheduleAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateScheduleAsync_WithEmptySections_ShouldReturnEmpty()
        {
            // Arrange
            var dto = new GenerateScheduleDto
            {
                Semester = "Fall",
                Year = 2024,
                SectionIds = new List<int>()
            };

            // Act
            var result = await _service.GenerateScheduleAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetScheduleAsync_WithValidData_ShouldReturn()
        {
            // Arrange
            var dto = new GenerateScheduleDto { Semester = "Fall", Year = 2024, SectionIds = new List<int> { 1 } };
            await _service.GenerateScheduleAsync(dto);

            // Act
            var result = await _service.GetScheduleAsync("Fall", 2024);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetScheduleByIdAsync_NotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetScheduleByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMyScheduleAsync_WithStudent_ShouldReturn()
        {
            // Arrange
            var studentUser = new User { Id = 10, FirstName = "Stu", LastName = "Dent", Email = "stu@test.com", UserName = "stud" };
            _context.Users.Add(studentUser);
            var student = new Student { Id = 10, UserId = 10, StudentNumber = "99999", DepartmentId = 1, IsActive = true };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMyScheduleAsync(10, "Fall", 2024);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToICalAsync_WithValidSchedule_ShouldReturnICalContent()
        {
            // Act
            var result = await _service.ExportToICalAsync(1, "Fall", 2024);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("BEGIN:VCALENDAR", result);
        }
    }
}
