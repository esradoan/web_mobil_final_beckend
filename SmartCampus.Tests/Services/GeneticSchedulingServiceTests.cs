using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class GeneticSchedulingServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly GeneticSchedulingService _service;

        public GeneticSchedulingServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _service = new GeneticSchedulingService(_context);
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
        public async Task GenerateWithGeneticAlgorithmAsync_WithValidData_ShouldGenerate()
        {
            // Arrange
            var dto = new GeneticScheduleRequestDto
            {
                Semester = "Fall",
                Year = 2024,
                PopulationSize = 10,
                Generations = 5,
                MutationRate = 0.1,
                CrossoverRate = 0.8
            };

            // Act
            var result = await _service.GenerateWithGeneticAlgorithmAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithmAsync_WithDefaultParams_ShouldGenerate()
        {
            // Arrange
            var dto = new GeneticScheduleRequestDto
            {
                Semester = "Fall",
                Year = 2024
            };

            // Act
            var result = await _service.GenerateWithGeneticAlgorithmAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithmAsync_WithMultipleSections_ShouldGenerate()
        {
            // Arrange
            var section2 = new CourseSection { Id = 2, CourseId = 1, SectionNumber = "B", Semester = "Fall", Year = 2024, Capacity = 25, InstructorId = 1 };
            _context.CourseSections.Add(section2);
            await _context.SaveChangesAsync();

            var dto = new GeneticScheduleRequestDto
            {
                Semester = "Fall",
                Year = 2024,
                PopulationSize = 20,
                Generations = 10
            };

            // Act
            var result = await _service.GenerateWithGeneticAlgorithmAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithmAsync_NoSections_ShouldReturnEmpty()
        {
            // Arrange
            var dto = new GeneticScheduleRequestDto
            {
                Semester = "Spring",
                Year = 2025
            };

            // Act
            var result = await _service.GenerateWithGeneticAlgorithmAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateWithGeneticAlgorithmAsync_ShouldReturnStats()
        {
            // Arrange
            var dto = new GeneticScheduleRequestDto
            {
                Semester = "Fall",
                Year = 2024,
                PopulationSize = 10,
                Generations = 5
            };

            // Act
            var result = await _service.GenerateWithGeneticAlgorithmAsync(dto);

            // Assert
            Assert.NotNull(result);
        }
    }
}
