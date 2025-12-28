using Microsoft.EntityFrameworkCore;
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
    public class CourseServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly CourseService _courseService;
        private readonly DbContextOptions<CampusDbContext> _options;

        public CourseServiceTests()
        {
            _options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(_options);
            // _context.Database.EnsureCreated();
            _courseService = new CourseService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateCourseAsync_ShouldCreateCourse_WhenValidDto()
        {
            // Arrange
            var dto = new CreateCourseDto
            {
                Code = "CS101",
                Name = "Intro to CS",
                Credits = 3,
                Ects = 5,
                DepartmentId = 1
            };

            // Act
            var result = await _courseService.CreateCourseAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CS101", result.Code);
            Assert.Equal("Intro to CS", result.Name);
            Assert.NotEqual(0, result.Id);
            
            var dbCourse = await _context.Courses.FindAsync(result.Id);
            Assert.NotNull(dbCourse);
        }

        [Fact]
        public async Task GetCoursesAsync_ShouldReturnCourses_WhenCoursesExist()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);
            _context.Courses.AddRange(
                new Course { Id = 2001, Code = "CS101", Name = "Intro to CS", Credits = 3, Ects = 5, IsDeleted = false, DepartmentId = 1 },
                new Course { Id = 2002, Code = "MATH101", Name = "Calculus I", Credits = 4, Ects = 6, IsDeleted = false, DepartmentId = 1 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.GetCoursesAsync(1, 10, null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Data.Count >= 1, $"Expected at least 1 course, found {result.Data.Count}");
        }

        [Fact]
        public async Task GetCourseByIdAsync_ShouldReturnCourse_WhenCourseExists()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);
            var course = new Course { Id = 2001, Code = "CS101", Name = "Intro to CS", Credits = 3, Ects = 5, DepartmentId = 1 };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            // Act
            var result = await _courseService.GetCourseByIdAsync(course.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CS101", result.Code);
        }

        [Fact]
        public async Task UpdateCourseAsync_ShouldUpdateCourse_WhenCourseExists()
        {
            // Arrange
            var dept = new Department { Id = 2002, Name = "CS", Code = "CS", FacultyName = "Engineering" };
            _context.Departments.Add(dept);
            var course = new Course { Id = 2002, Code = "CS101", Name = "Old Name", Credits = 3, Ects = 5, DepartmentId = 2002 };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCourseDto
            {
                Name = "New Name",
                Credits = 4
            };

            // Act
            var result = await _courseService.UpdateCourseAsync(course.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            Assert.Equal(4, result.Credits);

            var dbCourse = await _context.Courses.FindAsync(course.Id);
            Assert.Equal("New Name", dbCourse.Name);
        }

        [Fact]
        public async Task DeleteCourseAsync_ShouldSoftDeleteCourse_WhenCourseExists()
        {
            // Arrange
            var course = new Course { Code = "CS101", Name = "Intro to CS", Credits = 3, Ects = 5, IsDeleted = false };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.DeleteCourseAsync(course.Id);

            // Assert
            Assert.True(result);
            
            var dbCourse = await _context.Courses.FindAsync(course.Id);
            Assert.True(dbCourse.IsDeleted);
        }


        [Fact]
        public async Task GetCoursesAsync_ShouldFilterAndSort()
        {
            // Arrange
            _context.Departments.Add(new Department { Id = 1, Name = "CS" });
            _context.Courses.AddRange(
                new Course { Id = 3001, Code = "CS101", Name = "Intro", Credits = 3, DepartmentId = 1, IsDeleted = false },
                new Course { Id = 3002, Code = "CS102", Name = "Advanced", Credits = 4, DepartmentId = 1, IsDeleted = false },
                new Course { Id = 3003, Code = "MATH101", Name = "Calc", Credits = 3, DepartmentId = 2, IsDeleted = false }
            );
            await _context.SaveChangesAsync();

            // Act - Search
            var searchResult = await _courseService.GetCoursesAsync(1, 10, "Intro", null, null);
            Assert.Single(searchResult.Data);
            Assert.Equal("CS101", searchResult.Data[0].Code);

            // Act - Filter Department
            var deptResult = await _courseService.GetCoursesAsync(1, 10, null, 1, null);
            Assert.Equal(2, deptResult.Data.Count);

            // Act - Sort Credits Descending
            var sortResult = await _courseService.GetCoursesAsync(1, 10, null, 1, "credits");
            Assert.Equal("CS102", sortResult.Data[0].Code); // 4 credits > 3 credits
        }

        [Fact]
        public async Task CreateSectionAsync_ShouldCreateSection()
        {
            // Arrange
            var course = new Course { Id = 4001, Code = "CS101", Name = "Intro" };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var dto = new CreateSectionDto
            {
                CourseId = 4001,
                SectionNumber = "01",
                Semester = "Fall",
                Year = 2024,
                Capacity = 30,
                ScheduleJson = "[{\"Day\":1}]"
            };

            // Act
            var result = await _courseService.CreateSectionAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4001, result.CourseId);
            Assert.Equal("01", result.SectionNumber);
            
            var dbSection = await _context.CourseSections.FindAsync(result.Id);
            Assert.NotNull(dbSection);
        }

        [Fact]
        public async Task UpdateSectionAsync_ShouldUpdate_WhenExists()
        {
            // Arrange
            var course = new Course { Id = 1, Name = "Test", Code = "T" };
            var classroom = new Classroom { Id = 1, RoomNumber = "101", Building = "A" };
            var instructor = new User { Id = 1, FirstName = "Inst", LastName = "Test" };
            _context.Courses.Add(course);
            _context.Classrooms.Add(classroom);
            _context.Users.Add(instructor);
            var section = new CourseSection { Id = 5001, CourseId = 1, SectionNumber = "01", Capacity = 30, ClassroomId = 1, InstructorId = 1, IsDeleted = false };
            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateSectionDto { Capacity = 50 };

            // Act
            var result = await _courseService.UpdateSectionAsync(5001, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Capacity);
            
            var dbSection = await _context.CourseSections.FindAsync(5001);
            Assert.Equal(50, dbSection.Capacity);
        }

        [Fact]
        public async Task DeleteSectionAsync_ShouldSoftDelete()
        {
            // Arrange
            var section = new CourseSection { Id = 6001, SectionNumber = "01", IsDeleted = false };
            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.DeleteSectionAsync(6001);

            // Assert
            Assert.True(result);
            var dbSection = await _context.CourseSections.FindAsync(6001);
            Assert.True(dbSection.IsDeleted);
        }

        [Fact]
        public async Task GetSectionsAsync_ShouldFilter()
        {
            // Arrange
            var course = new Course { Id = 1, Name = "Test", Code = "T" };
            var classroom = new Classroom { Id = 1, RoomNumber = "101", Building = "A" };
            var instructor = new User { Id = 1, FirstName = "Inst", LastName = "Test" };
            _context.Courses.Add(course);
            _context.Classrooms.Add(classroom);
            _context.Users.Add(instructor);
            _context.CourseSections.AddRange(
                new CourseSection { Id = 7001, Semester = "Fall", Year = 2024, CourseId = 1, ClassroomId = 1, InstructorId = 1, IsDeleted = false },
                new CourseSection { Id = 7002, Semester = "Spring", Year = 2024, CourseId = 1, IsDeleted = false }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.GetSectionsAsync("Fall", 2024, null, null);

            // Assert
            Assert.Single(result);
            Assert.Equal(7001, result[0].Id);
        }
        
        [Fact]
        public async Task GetSectionByIdAsync_ShouldReturnSection()
        {
            // Arrange
            var course = new Course { Id = 1, Name = "Test", Code = "T" };
            var classroom = new Classroom { Id = 1, RoomNumber = "101", Building = "A" };
            var instructor = new User { Id = 1, FirstName = "Inst", LastName = "Test" };
            _context.Courses.Add(course);
            _context.Classrooms.Add(classroom);
            _context.Users.Add(instructor);
            var section = new CourseSection { Id = 8001, SectionNumber = "01", IsDeleted = false, CourseId = 1, ClassroomId = 1, InstructorId = 1 };
            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.GetSectionByIdAsync(8001);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8001, result.Id);
        }
    }
}
