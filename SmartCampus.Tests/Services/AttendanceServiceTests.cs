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
    public class AttendanceServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly AttendanceService _attendanceService;

        public AttendanceServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _context.Database.EnsureCreated();
            _mockNotificationService = new Mock<INotificationService>();
            
            _attendanceService = new AttendanceService(_context, _mockNotificationService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldCreateSession_WhenValidRequest()
        {
            // Arrange
            var instructorId = 1010;
            var sectionId = 1001;
            
            // Classroom ID 1 exists in Seed Data, so we can use it or use > 1000 if we ADD it.
            // But we add it: _context.Classrooms.Add(classroom).
            var classroom = new Classroom { Id = 1001, Latitude = 40.0m, Longitude = 29.0m, RoomNumber = "Lab 1", Building = "A" };
            var course = new Course { Id = 1001, Name = "Course 1", Code = "C1" };
            var section = new CourseSection 
            { 
                Id = sectionId, 
                InstructorId = instructorId, 
                ClassroomId = 1001, 
                IsDeleted = false,
                CourseId = 1001
            };
            
            _context.Classrooms.Add(classroom);
            _context.Courses.Add(course);
            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            var dto = new CreateAttendanceSessionDto
            {
                SectionId = sectionId,
                Date = DateTime.UtcNow,
                StartTime = TimeSpan.Parse("09:00"),
                EndTime = TimeSpan.Parse("10:30"),
                GeofenceRadius = 20
            };

            // Act
            var result = await _attendanceService.CreateSessionAsync(instructorId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("active", result.Status);
            Assert.Equal(instructorId, result.InstructorId);
            
            var dbSession = await _context.AttendanceSessions.FindAsync(result.Id);
            Assert.NotNull(dbSession);
            Assert.Equal(20, dbSession.GeofenceRadius);
        }

        [Fact]
        public async Task GetActiveSessionsForStudentAsync_ShouldReturnSessions_WhenStudentEnrolled()
        {
            // Arrange
            var studentUserId = 1001;
            var studentId = 1000; // Entity Id
            var sectionId = 1005;
            var courseId = 1099;

            var student = new Student { Id = studentId, UserId = studentUserId, DepartmentId = 1 };
            var course = new Course { Id = courseId, Name = "Test Course", Code = "TEST101" };
            var section = new CourseSection 
            { 
                Id = sectionId, 
                CourseId = courseId, 
                Semester = "Fall", 
                Year = 2024,
                IsDeleted = false
            };
            // Note: Enrollment connects Student.Id (1000) and Section.Id (1005)
            var enrollment = new Enrollment { Id = 1000, StudentId = studentId, SectionId = sectionId, Status = "enrolled" };
            
            var session = new AttendanceSession
            {
                Id = 1500,
                SectionId = sectionId,
                Status = "active",
                Date = DateTime.UtcNow.AddMinutes(1), // Future/Present
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(12),
                GeofenceRadius = 15,
                InstructorId = 10,
                QrCode = "QR",
                QrCodeExpiry = DateTime.UtcNow.AddHours(1)
            };

            _context.Students.Add(student);
            _context.Courses.Add(course);
            _context.CourseSections.Add(section);
            _context.Enrollments.Add(enrollment);
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.GetActiveSessionsForStudentAsync(studentUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1500, result[0].Id);
        }
    }
}
