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
        }

        [Fact]
        public async Task GetActiveSessionsForStudentAsync_ShouldReturnSessions_WhenStudentEnrolled()
        {
            // Arrange
            var studentUserId = 1001;
            var studentId = 1000;
            var sectionId = 1005;
            var courseId = 1099;

            var student = new Student { Id = studentId, UserId = studentUserId, DepartmentId = 1, IsActive = true };
            var course = new Course { Id = courseId, Name = "Test Course", Code = "TEST101" };
            var section = new CourseSection 
            { 
                Id = sectionId, 
                CourseId = courseId, 
                Semester = "Fall", 
                Year = 2024,
                IsDeleted = false
            };
            var enrollment = new Enrollment { Id = 1000, StudentId = studentId, SectionId = sectionId, Status = "enrolled" };
            
            var session = new AttendanceSession
            {
                Id = 1500,
                SectionId = sectionId,
                Status = "active",
                Date = DateTime.UtcNow.Date.AddHours(1), // Today and Future
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

        [Fact]
        public async Task GetSessionByIdAsync_ShouldReturnSession_WhenExists()
        {
            // Arrange
            var sessionId = 2001;
            var instructorId = 2002;
            var sectionId = 2003;
            var courseId = 2004;
            var classroomId = 101; // Avoid 1-5

            var instructor = new User { Id = instructorId, FirstName = "Inst", LastName = "Test" };
            var classroom = new Classroom { Id = classroomId, RoomNumber = "101", Building = "A" };
            var course = new Course { Id = courseId, Name = "Test Course", Code = "TC101" };
            var section = new CourseSection 
            { 
                Id = sectionId, 
                CourseId = courseId, 
                InstructorId = instructorId,
                ClassroomId = classroomId,
                IsDeleted = false
            };

            var session = new AttendanceSession
            {
                Id = sessionId,
                SectionId = sectionId,
                InstructorId = instructorId,
                Status = "active",
                Date = DateTime.UtcNow,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10)
            };
            
            _context.Users.Add(instructor);
            _context.Classrooms.Add(classroom);
            _context.Courses.Add(course);
            _context.CourseSections.Add(section);
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.GetSessionByIdAsync(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.Id);
            Assert.Equal(instructorId, result.InstructorId);
        }

        [Fact]
        public async Task CloseSessionAsync_ShouldCloseSession_WhenAuthorized()
        {
            // Arrange
            var sessionId = 3001;
            var instructorId = 3002;
            var session = new AttendanceSession
            {
                Id = sessionId,
                InstructorId = instructorId,
                Status = "active"
            };
            
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.CloseSessionAsync(sessionId, instructorId);

            // Assert
            Assert.True(result);
            var dbSession = await _context.AttendanceSessions.FindAsync(sessionId);
            Assert.Equal("closed", dbSession?.Status);
        }

        [Fact]
        public async Task GetMySessionsAsync_ShouldReturnInstructorSessions()
        {
            // Arrange
            var instructorId = 4001;
            var otherInstructorId = 4002;
            var sectionId = 4003;
            var courseId = 4004;
            var classroomId = 101; // Avoid 1-5

            var course = new Course { Id = courseId, Name = "C", Code = "C" };
            var classroom = new Classroom { Id = classroomId, RoomNumber = "R", Building = "B" };
            var section = new CourseSection { Id = sectionId, CourseId = courseId, ClassroomId = classroomId, IsDeleted = false };

            _context.Courses.Add(course);
            _context.Classrooms.Add(classroom); // Now passes
            _context.CourseSections.Add(section);
            
            _context.AttendanceSessions.AddRange(
                new AttendanceSession { Id = 401, InstructorId = instructorId, SectionId = sectionId, Date = DateTime.UtcNow },
                new AttendanceSession { Id = 402, InstructorId = instructorId, SectionId = sectionId, Date = DateTime.UtcNow.AddDays(-1) },
                new AttendanceSession { Id = 403, InstructorId = otherInstructorId, SectionId = sectionId, Date = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.GetMySessionsAsync(instructorId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal(instructorId, s.InstructorId));
        }

        [Fact]
        public async Task CheckInAsync_ShouldSucceed_WhenValid()
        {
            // Arrange
            var sessionId = 5001;
            var studentUserId = 5002;
            var studentId = 50020; 
            
            var student = new Student { Id = studentId, UserId = studentUserId, IsActive = true };
            _context.Students.Add(student);

            var session = new AttendanceSession
            {
                Id = sessionId,
                Status = "active",
                Latitude = 40.0m,
                Longitude = 29.0m,
                GeofenceRadius = 50,
                SectionId = 1
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            var request = new CheckInRequestDto
            {
                Latitude = 40.0m, // Exact location
                Longitude = 29.0m,
                Accuracy = 10,
                DeviceType = "mobile"
            };

            // Act
            var result = await _attendanceService.CheckInAsync(sessionId, studentUserId, request, "10.0.0.5");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsFlagged);
            Assert.Equal("Check-in successful", result.Message);
            
            var record = await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.StudentId == studentUserId);
            Assert.NotNull(record);
        }

        [Fact]
        public async Task CheckInAsync_ShouldFail_WhenTooFar()
        {
            // Arrange
            var sessionId = 6001;
            var studentUserId = 6002;
            
            var student = new Student { Id = 60020, UserId = studentUserId, IsActive = true };
            _context.Students.Add(student);

            var session = new AttendanceSession
            {
                Id = sessionId,
                Status = "active",
                Latitude = 40.0m,
                Longitude = 29.0m,
                GeofenceRadius = 50,
                SectionId = 1
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            // Far away coordinates
            var request = new CheckInRequestDto
            {
                Latitude = 41.0m, 
                Longitude = 29.0m,
                Accuracy = 10
            };

            // Act
            var result = await _attendanceService.CheckInAsync(sessionId, studentUserId, request, "10.0.0.5");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsFlagged); 
            Assert.Contains("too far", result.Message);
            
            var record = await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.StudentId == studentUserId);
            Assert.NotNull(record);
            Assert.True(record.IsFlagged);
        }

        [Fact]
        public async Task CheckInAsync_ShouldThrow_WhenDuplicate()
        {
            // Arrange
            var sessionId = 7001;
            var studentUserId = 7002;
            
            var student = new Student { Id = 70020, UserId = studentUserId, IsActive = true };
            _context.Students.Add(student);

            var session = new AttendanceSession { Id = sessionId, Status = "active", SectionId = 1, Latitude=40, Longitude=29, GeofenceRadius=50 };
            _context.AttendanceSessions.Add(session);
            
            _context.AttendanceRecords.Add(new AttendanceRecord { SessionId = sessionId, StudentId = studentUserId, CheckInTime = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var request = new CheckInRequestDto { Latitude = 40, Longitude = 29 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _attendanceService.CheckInAsync(sessionId, studentUserId, request, "10.0.0.1"));
        }

        [Fact]
        public async Task CreateExcuseRequestAsync_ShouldCreateRequest()
        {
            // Arrange
            var studentUserId = 8001;
            var sessionId = 8002;
            
            var requestDto = new CreateExcuseRequestDto 
            { 
                SessionId = sessionId, 
                Reason = "Sick" 
            };

            // Act
            var result = await _attendanceService.CreateExcuseRequestAsync(studentUserId, requestDto, "doc.pdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("pending", result.Status);
            Assert.Equal(studentUserId, result.StudentId);
            
            var dbReq = await _context.ExcuseRequests.FirstOrDefaultAsync(e => e.StudentId == studentUserId && e.SessionId == sessionId);
            Assert.NotNull(dbReq);
            Assert.Equal("doc.pdf", dbReq.DocumentUrl);
        }

        [Fact]
        public async Task ApproveExcuseAsync_ShouldApproveAndNotify()
        {
            // Arrange
            var requestId = 9001;
            var reviewerId = 9002;
            var studentId = 9003;
            var sessionId = 9004;
            
            var excuse = new ExcuseRequest 
            { 
                Id = requestId, 
                StudentId = studentId, 
                SessionId = sessionId, 
                Status = "pending" 
            };
            _context.ExcuseRequests.Add(excuse);
            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.ApproveExcuseAsync(requestId, reviewerId, "Ok");

            // Assert
            Assert.True(result);
            var dbExcuse = await _context.ExcuseRequests.FindAsync(requestId);
            Assert.Equal("approved", dbExcuse.Status);
            Assert.Equal(reviewerId, dbExcuse.ReviewedBy);
            
            _mockNotificationService.Verify(n => n.SendExcuseApprovedAsync(studentId, sessionId), Times.Once);
        }

        [Fact]
        public async Task GetMyAttendanceAsync_ShouldReturnCorrectStats()
        {
            // Arrange
            var studentUserId = 10001;
            var sectionId = 10002;
            
            // Student
            _context.Students.Add(new Student { Id = 100010, UserId = studentUserId });
            
            // Section & Course
            var course = new Course { Id = 100, Code = "CS101", Name = "Intro" };
            var section = new CourseSection { Id = sectionId, CourseId = 100, SectionNumber = "01" };
            _context.Courses.Add(course);
            _context.CourseSections.Add(section);

            // Enrollment
            _context.Enrollments.Add(new Enrollment { StudentId = studentUserId, SectionId = sectionId, Status = "enrolled" });

            // Sessions (Total 4)
            for(int i=0; i<4; i++) 
                _context.AttendanceSessions.Add(new AttendanceSession { Id = 1010+i, SectionId = sectionId, Status="closed" });
            
            // Attendance (Attended 2)
            _context.AttendanceRecords.Add(new AttendanceRecord { SessionId = 1010, StudentId = studentUserId });
            _context.AttendanceRecords.Add(new AttendanceRecord { SessionId = 1011, StudentId = studentUserId });

            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.GetMyAttendanceAsync(studentUserId);

            // Assert
            Assert.Single(result);
            var stat = result[0];
            Assert.Equal(4, stat.TotalSessions);
            Assert.Equal(2, stat.AttendedSessions);
            Assert.Equal(50.0m, stat.AttendancePercentage); // 2/4 = 50%
            Assert.Equal("Warning", stat.Status); // 50% is Warning threshold
        }

        [Fact]
        public async Task GetAttendanceReportAsync_ShouldReturnReportForSection()
        {
             // Arrange
            var sectionId = 11001;
            var studentId1 = 11002;
            var studentId2 = 11003;
            var instructorId = 11004;

            var course = new Course { Id = 2100, Name="Math", Code="MAT101" };
            var instructor = new User { Id = instructorId, FirstName = "Inst", LastName = "Report" };
            var section = new CourseSection 
            { 
                Id = sectionId, 
                CourseId = 2100, 
                InstructorId = instructorId,
                SectionNumber = "01",
                IsDeleted = false 
            };
            
            _context.Courses.Add(course);
            _context.Users.Add(instructor);
            _context.CourseSections.Add(section);

            // 2 Students enrolled
            _context.Enrollments.AddRange(
                new Enrollment { StudentId = studentId1, SectionId = sectionId, Status = "enrolled" },
                new Enrollment { StudentId = studentId2, SectionId = sectionId, Status = "enrolled" }
            );
            
            // Students in Users/Students table
            _context.Users.AddRange(
                new User { Id = studentId1, FirstName="S1", LastName="L1" },
                new User { Id = studentId2, FirstName="S2", LastName="L2" }
            );
            _context.Students.AddRange(
                new Student { UserId = studentId1, StudentNumber="111" },
                new Student { UserId = studentId2, StudentNumber="222" }
            );

            // 2 Sessions
            _context.AttendanceSessions.AddRange(
                new AttendanceSession { Id = 1110, SectionId = sectionId },
                new AttendanceSession { Id = 1111, SectionId = sectionId }
            );

            // Student 1 attended both, Student 2 attended none
            _context.AttendanceRecords.AddRange(
                new AttendanceRecord { SessionId = 1110, StudentId = studentId1 },
                new AttendanceRecord { SessionId = 1111, StudentId = studentId1 }
            );

            await _context.SaveChangesAsync();

            // Act
            var result = await _attendanceService.GetAttendanceReportAsync(sectionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Students.Count);
            
            var s1 = result.Students.FirstOrDefault(s => s.StudentId == studentId1);
            Assert.Equal(100.0m, s1?.AttendancePercentage);
            
            var s2 = result.Students.FirstOrDefault(s => s.StudentId == studentId2);
            Assert.Equal(0.0m, s2?.AttendancePercentage);
        }
    }
}
