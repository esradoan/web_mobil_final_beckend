using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class EnrollmentServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IGradeCalculationService> _mockGradeService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly EnrollmentService _service;

        public EnrollmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _mockGradeService = new Mock<IGradeCalculationService>();
            _mockNotificationService = new Mock<INotificationService>();

            // Mock GradeService to return valid values to avoid null exceptions in other logic
            _mockGradeService.Setup(g => g.CalculateLetterGrade(It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
                .Returns("AA");
            _mockGradeService.Setup(g => g.CalculateGradePoint("AA")).Returns(4.0m);

            _service = new EnrollmentService(_context, _mockGradeService.Object, _mockNotificationService.Object);
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

            var student = new Student { Id = 1, UserId = 1, StudentNumber = "12345", DepartmentId = 1, IsActive = true };
            _context.Students.Add(student);

            var instructor = new User { Id = 2, FirstName = "Prof", LastName = "Test", Email = "prof@test.com", UserName = "prof" };
            _context.Users.Add(instructor);

            var section = new CourseSection { Id = 1, CourseId = 1, SectionNumber = "A", Semester = "Fall", Year = 2024, Capacity = 30, InstructorId = 2 };
            _context.CourseSections.Add(section);

            _context.SaveChanges();
        }

        private async Task<Enrollment> CreateEnrollmentManually()
        {
            var enrollment = new Enrollment
            {
                Id = 100,
                SectionId = 1,
                StudentId = 1,
                Status = "enrolled",
                EnrollmentDate = DateTime.UtcNow
            };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // Note: EnrollAsync uses ExecuteUpdate which is not supported by InMemory DB
        // Testing the service validation logic instead

        [Fact]
        public async Task EnrollAsync_StudentNotFound_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(999, 1));
        }

        [Fact]
        public async Task EnrollAsync_InactiveStudent_ShouldThrow()
        {
            // Arrange
            var user = new User { Id = 10, FirstName = "Inactive", LastName = "Student", Email = "inactive@test.com" };
            _context.Users.Add(user);
            var student = new Student { Id = 10, UserId = 10, StudentNumber = "S10", IsActive = false, DepartmentId = 1 };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(10, 1));
            Assert.Contains("Pasif öğrenciler", ex.Message);
        }

        [Fact]
        public async Task EnrollAsync_SectionNotFound_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.EnrollAsync(1, 999));
        }

        [Fact]
        public async Task EnrollAsync_CrossDepartment_ShouldThrow_WhenNotAllowed()
        {
            // Arrange
            // Create a different department
            var otherDept = new Department { Id = 2, Name = "Other Dept", Code = "OD" };
            _context.Departments.Add(otherDept);
            
            // Create a course in that department that doesn't allow cross-dept
            var otherCourse = new Course { Id = 2, Name = "Other Course", Code = "OD101", DepartmentId = 2, AllowCrossDepartment = false, Type = CourseType.Required };
            _context.Courses.Add(otherCourse);

            // Create a section for that course
            var otherSection = new CourseSection { Id = 2, CourseId = 2, SectionNumber = "A", Semester = "Fall", Year = 2024, Capacity = 30 };
            _context.CourseSections.Add(otherSection);

            await _context.SaveChangesAsync();

            // Student (Id 1, Dept 1) tries to enroll in Course (Id 2, Dept 2)
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(1, 2)); // Student Id 1 is UserId 1 in seed
            Assert.Contains("sadece Other Dept bölümü öğrencileri için açıktır", ex.Message);
        }

        [Fact]
        public async Task EnrollAsync_AlreadyEnrolled_ShouldThrow()
        {
            // Arrange
            var sectionId = 1;
            var studentId = 1; // UserId
            // SeedData already creates user 1, student 1 (Id 1, UserId 1) 
            // Create existing enrollment
            var enrollment = new Enrollment { StudentId = 1, SectionId = sectionId, Status = "enrolled", EnrollmentDate = DateTime.UtcNow };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(studentId, sectionId));
            Assert.Contains("Already enrolled", ex.Message);
        }

        [Fact]
        public async Task EnrollAsync_PrerequisiteNotMet_ShouldThrow()
        {
            // Arrange
            var prereqCourse = new Course { Id = 3, Name = "Prereq", Code = "PRE101", DepartmentId = 1, Credits = 3 };
            var targetCourse = new Course { Id = 4, Name = "Advanced", Code = "ADV102", DepartmentId = 1, Credits = 3 };
            _context.Courses.AddRange(prereqCourse, targetCourse);
            
            var prereq = new CoursePrerequisite { CourseId = 4, PrerequisiteCourseId = 3 };
            _context.CoursePrerequisites.Add(prereq);

            var targetSection = new CourseSection { Id = 3, CourseId = 4, SectionNumber = "A", Semester = "Spring", Year = 2025, Capacity = 30 };
            _context.CourseSections.Add(targetSection);
            
            await _context.SaveChangesAsync();

            // Act & Assert (Student 1 has no previous enrollments)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(1, 3));
            Assert.Contains("Prerequisite not met", ex.Message);
        }

        [Fact]
        public async Task EnrollAsync_ScheduleConflict_ShouldThrow()
        {
            // Arrange
            // Existing enrollment: Monday 09:00-10:30
            var course1 = new Course { Id = 5, Name = "C1", Code = "C1", DepartmentId = 1 };
            var section1 = new CourseSection { 
                Id = 4, CourseId = 5, Semester = "Fall", Year = 2024, 
                ScheduleJson = "{\"Monday\":[\"09:00-10:30\"]}" 
            };
            
            // New course: Monday 10:00-11:30 (Overlap)
            var course2 = new Course { Id = 6, Name = "C2", Code = "C2", DepartmentId = 1 };
            var section2 = new CourseSection { 
                Id = 5, CourseId = 6, Semester = "Fall", Year = 2024, 
                ScheduleJson = "{\"Monday\":[\"10:00-11:30\"]}" 
            };
            
            _context.Courses.AddRange(course1, course2);
            _context.CourseSections.AddRange(section1, section2);
            
            var enrollment = new Enrollment { StudentId = 1, SectionId = 4, Status = "enrolled", EnrollmentDate = DateTime.UtcNow };
            _context.Enrollments.Add(enrollment);
            
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(1, 5));
            Assert.Contains("Schedule conflict", ex.Message);
        }

        [Fact]
        public async Task DropCourseAsync_NotEnrolled_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DropCourseAsync(999, 1);

            // Assert
            Assert.False(result);
        }

        // Note: DropCourseAsync_WithValidEnrollment test removed because
        // DropCourseAsync uses ExecuteUpdate which is not supported by EF Core InMemory provider        [Fact]
        [Fact]
        public async Task GetMyCoursesAsync_NoEnrollments_ShouldReturnEmpty()
        {
            // Act
            var result = await _service.GetMyCoursesAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMyCoursesAsync_WithEnrollments_ShouldReturnCourses()
        {
            // Arrange
            await CreateEnrollmentManually();

            // Act
            var result = await _service.GetMyCoursesAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetSectionStudentsAsync_WithStudents_ShouldReturnStudents()
        {
            // Arrange
            await CreateEnrollmentManually();

            // Act
            var result = await _service.GetSectionStudentsAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetMyGradesAsync_WithEnrollment_ShouldReturnGrades()
        {
            // Arrange
            await CreateEnrollmentManually();

            // Act
            var result = await _service.GetMyGradesAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTranscriptAsync_WithEnrollments_ShouldReturnTranscript()
        {
            // Arrange
            await CreateEnrollmentManually();

            // Act
            var result = await _service.GetTranscriptAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task EnterGradeAsync_WithValidData_ShouldEnterGrade()
        {
            // Arrange
            var enrollment = await CreateEnrollmentManually();

            var gradeDto = new GradeInputDto
            {
                EnrollmentId = enrollment.Id,
                MidtermGrade = 80,
                FinalGrade = 90,
                HomeworkGrade = 85
            };

            // Act
            var result = await _service.EnterGradeAsync(2, gradeDto);

            // Assert
            Assert.NotNull(result);
        }
        [Fact]
        public async Task GetSectionStudentsAsync_NoEnrolledButCompleted_ShouldReturnStudents()
        {
            // Arrange
            // Create completed enrollment
            var enrollment = new Enrollment { Id = 200, SectionId = 1, StudentId = 1, Status = "completed", EnrollmentDate = DateTime.UtcNow, LetterGrade = "AA" };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSectionStudentsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("AA", result[0].LetterGrade);
        }
    }
}
