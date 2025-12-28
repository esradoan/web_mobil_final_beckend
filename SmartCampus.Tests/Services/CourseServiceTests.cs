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
    }
}
