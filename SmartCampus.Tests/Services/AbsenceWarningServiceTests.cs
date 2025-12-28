#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class AbsenceWarningServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AbsenceWarningService>> _mockLogger;
        private readonly IServiceProvider _serviceProvider;

        public AbsenceWarningServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AbsenceWarningService>>();

            // Setup ServiceProvider for DI
            var services = new ServiceCollection();
            services.AddScoped<CampusDbContext>(_ => _context);
            services.AddScoped<IEmailService>(_ => _mockEmailService.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        private async Task SetupTestDataAsync(bool isCurrentSemester = true)
        {
            var department = new Department { Id = 1, Name = "Computer Science", Code = "CS" };
            _context.Departments.Add(department);

            var course = new Course { Id = 1, Name = "Programming 101", Code = "CS101", Credits = 3, DepartmentId = 1 };
            _context.Courses.Add(course);

            var student = new User { Id = 1, Email = "student@test.com", FirstName = "Test", LastName = "Student" };
            _context.Users.Add(student);

            var instructor = new User { Id = 2, Email = "instructor@test.com", FirstName = "Test", LastName = "Instructor" };
            _context.Users.Add(instructor);

            var classroom = new Classroom { Id = 1, Building = "A", RoomNumber = "101", Capacity = 30 };
            _context.Classrooms.Add(classroom);

            // Determine semester
            var currentMonth = DateTime.Now.Month;
            var semester = currentMonth >= 9 || currentMonth <= 1 ? "Fall" : "Spring";
            if (!isCurrentSemester) semester = semester == "Fall" ? "Spring" : "Fall"; // Flip it

            var section = new CourseSection 
            { 
                Id = 1, 
                CourseId = 1, 
                SectionNumber = "001",
                Semester = semester,
                Year = DateTime.Now.Year,
                InstructorId = 2,
                ClassroomId = 1,
                Capacity = 30
            };
            _context.CourseSections.Add(section);

            var enrollment = new Enrollment 
            { 
                Id = 1, 
                StudentId = 1, 
                SectionId = 1, 
                Status = "Active",
                EnrollmentDate = DateTime.UtcNow.AddDays(-30)
            };
            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();
        }

        private async Task ExecuteCheckAbsenceRatesAsync(AbsenceWarningService service)
        {
            var methodInfo = typeof(AbsenceWarningService).GetMethod("CheckAbsenceRatesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null) throw new InvalidOperationException("Method CheckAbsenceRatesAsync not found");
            
            var task = (Task)methodInfo.Invoke(service, null);
            await task;
        }

        [Fact]
        public async Task LowAbsence_ShouldNotSendEmail()
        {
            await SetupTestDataAsync();
            // 10 sessions, 9 attended -> 10% absence (Low)
            await CreateSessionsAndAttendance(10, 9);

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            await ExecuteCheckAbsenceRatesAsync(service);

            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task WarningLevel_ShouldSendWarningEmail()
        {
            await SetupTestDataAsync();
            // 10 sessions, 8 attended -> 20% absence (Warning)
            await CreateSessionsAndAttendance(10, 8);

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            await ExecuteCheckAbsenceRatesAsync(service);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                "student@test.com", 
                It.Is<string>(s => s.Contains("⚠️ Devamsızlık Uyarısı")), 
                It.Is<string>(b => b.Contains("%20"))), Times.Once);
        }

        [Fact]
        public async Task CriticalLevel_ShouldSendCriticalWarningEmail()
        {
            await SetupTestDataAsync();
            // 10 sessions, 7 attended -> 30% absence (Critical)
            await CreateSessionsAndAttendance(10, 7);

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            await ExecuteCheckAbsenceRatesAsync(service);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                "student@test.com", 
                It.Is<string>(s => s.Contains("🚨 KRİTİK")), 
                It.Is<string>(b => b.Contains("%30"))), Times.Once);
        }

        [Fact]
        public async Task ExcusedAbsences_ShouldReduceAbsenceRate()
        {
            await SetupTestDataAsync();
            // 10 sessions, 5 attended, 3 excused -> 8 effective -> 20% absence (Warning)
            await CreateSessionsAndAttendance(10, 5);
            
            // Add 3 excused
            for (int i = 6; i <= 8; i++)
            {
                _context.ExcuseRequests.Add(new ExcuseRequest { 
                    SessionId = i, StudentId = 1, Status = "Approved", Reason = "Medical" 
                });
            }
            await _context.SaveChangesAsync();

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            await ExecuteCheckAbsenceRatesAsync(service);

            // Should be Warning (20%), NOT Critical (50% raw)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                "student@test.com", 
                It.Is<string>(s => s.Contains("⚠️ Devamsızlık Uyarısı")), // Expect Warning
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SemesterMismatch_ShouldIgnoreEnrollment()
        {
            await SetupTestDataAsync(isCurrentSemester: false);
            // 10 sessions, 0 attended -> 100% absence (Critical) - BUT wrong semester
            await CreateSessionsAndAttendance(10, 0);

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            await ExecuteCheckAbsenceRatesAsync(service);

            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EmailFailure_ShouldLogWarning_AndNotThrow()
        {
            await SetupTestDataAsync();
            // 20% absence -> Trigger Warning
            await CreateSessionsAndAttendance(10, 8);

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP Error"));

            var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
            
            // Should not throw
            await ExecuteCheckAbsenceRatesAsync(service);

            // Verify logging
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email gönderilemedi")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
        
        [Fact]
        public async Task ExecuteAsync_ShouldRunLoop()
        {
             // This tests the BackgroundService loop roughly
             // We can't easily test the loop without cancellation token tricks or TimeProvider
             // But we can verify it starts and logs
             
             var cts = new CancellationTokenSource();
             cts.CancelAfter(50); // Cancel almost immediately
             
             var service = new AbsenceWarningService(_serviceProvider, _mockLogger.Object);
             
             // Just verifying it doesn't crush
             await service.StartAsync(cts.Token);
             await service.StopAsync(cts.Token);
             
             _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Service başlatıldı")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        private async Task CreateSessionsAndAttendance(int totalSessions, int attendedCount)
        {
            for (int i = 1; i <= totalSessions; i++)
            {
                _context.AttendanceSessions.Add(new AttendanceSession
                {
                    Id = i, SectionId = 1, InstructorId = 2, Status = "Closed",
                    Date = DateTime.UtcNow.AddDays(-i), StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero, QrCode = "qr"
                });
            }

            for (int i = 1; i <= attendedCount; i++)
            {
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    Id = i, SessionId = i, StudentId = 1, IsFlagged = false, CheckInTime = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
