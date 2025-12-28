using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class AttendanceAnalyticsServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly AttendanceAnalyticsService _service;

        public AttendanceAnalyticsServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _service = new AttendanceAnalyticsService(_context);
            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);

            var course = new Course { Id = 1, Name = "Intro to CS", Code = "CS101", Credits = 3, DepartmentId = 1 };
            _context.Courses.Add(course);

            var user = new User { Id = 1, FirstName = "Prof", LastName = "Test", Email = "prof@test.com", UserName = "prof" };
            _context.Users.Add(user);

            var section = new CourseSection { Id = 1, CourseId = 1, SectionNumber = "A", Semester = "Fall", Year = 2024, Capacity = 30, InstructorId = 1 };
            _context.CourseSections.Add(section);

            var studentUser = new User { Id = 2, FirstName = "Test", LastName = "Student", Email = "test@test.com", UserName = "test" };
            _context.Users.Add(studentUser);

            var student = new Student { Id = 1, UserId = 2, StudentNumber = "12345", DepartmentId = 1, IsActive = true };
            _context.Students.Add(student);

            var enrollment = new Enrollment { Id = 1, SectionId = 1, StudentId = 2, Status = "Active", EnrollmentDate = DateTime.UtcNow };
            _context.Enrollments.Add(enrollment);

            var session = new AttendanceSession { Id = 1, SectionId = 1, Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(12), Status = "completed" };
            _context.AttendanceSessions.Add(session);

            var record = new AttendanceRecord { Id = 1, SessionId = 1, StudentId = 2, CheckInTime = DateTime.Now };
            _context.AttendanceRecords.Add(record);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetAttendanceTrendsAsync_WithValidSection_ShouldReturnTrends()
        {
            // Act
            var result = await _service.GetAttendanceTrendsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SectionId);
        }

        [Fact]
        public async Task GetAttendanceTrendsAsync_WithInvalidSection_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetAttendanceTrendsAsync(999));
        }

        [Fact]
        public async Task GetStudentRiskAnalysisAsync_WithValidStudent_ShouldReturnAnalysis()
        {
            // Act
            var result = await _service.GetStudentRiskAnalysisAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.StudentId);
        }

        [Fact]
        public async Task GetSectionAnalyticsAsync_WithValidSection_ShouldReturnAnalytics()
        {
            // Act
            var result = await _service.GetSectionAnalyticsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SectionId);
        }

        [Fact]
        public async Task GetCampusAnalyticsAsync_ShouldReturnCampusStats()
        {
            // Act
            var result = await _service.GetCampusAnalyticsAsync();

            // Assert
            Assert.NotNull(result);
        }

        // PDF export tests skipped due to QuestPDF license validation in CI
        // Excel export should work as it uses simple CSV

        [Fact]
        public async Task ExportSectionReportToExcelAsync_WithValidSection_ShouldReturnBytes()
        {
            // Act
            var result = await _service.ExportSectionReportToExcelAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }
    }
}
