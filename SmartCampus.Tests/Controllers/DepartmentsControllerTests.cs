using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.DataAccess;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class DepartmentsControllerTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<IGenericRepository<Department>> _mockRepo;
        private readonly DepartmentsController _controller;

        public DepartmentsControllerTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _mockRepo = new Mock<IGenericRepository<Department>>();
            _controller = new DepartmentsController(_mockRepo.Object, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithDepartments()
        {
            // Arrange
            _context.Departments.Add(new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Eng", IsDeleted = false });
            _context.Departments.Add(new Department { Id = 2, Name = "Math", Code = "MATH", FacultyName = "Sci", IsDeleted = false });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenDepartmentExists()
        {
            // Arrange
            _context.Departments.Add(new Department { Id = 1, Name = "CS", Code = "CS", FacultyName = "Eng", IsDeleted = false });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
        {
            // Act
            var result = await _controller.GetById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
