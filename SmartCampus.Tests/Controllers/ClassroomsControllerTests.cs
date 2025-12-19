#nullable disable
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCampus.API.Controllers;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class ClassroomsControllerTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly ClassroomsController _controller;

        public ClassroomsControllerTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);
            _controller = new ClassroomsController(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetAllClassrooms_ReturnsOk_WithClassroomList()
        {
            // Arrange
            _context.Classrooms.Add(new Classroom { Id = 1, Building = "A", RoomNumber = "101", IsDeleted = false });
            _context.Classrooms.Add(new Classroom { Id = 2, Building = "B", RoomNumber = "102", IsDeleted = false });
            _context.Classrooms.Add(new Classroom { Id = 3, Building = "C", RoomNumber = "103", IsDeleted = true }); // Should be filtered out
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAllClassrooms();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Reflection to check data count if anonymous type or wrapped
            // Assuming the controller returns { data = [...] }
            dynamic val = okResult.Value;
            var dataProp = val.GetType().GetProperty("data");
            var data = dataProp.GetValue(val) as System.Collections.IEnumerable;
            
            int count = 0;
            foreach (var item in data) count++;
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetClassroomById_ReturnsOk_WhenClassroomExists()
        {
            // Arrange
            _context.Classrooms.Add(new Classroom 
            { 
                Id = 1, 
                Building = "A", 
                RoomNumber = "101", 
                Capacity = 50,
                FeaturesJson = "{}"
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetClassroomById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            dynamic val = okResult.Value;
            Assert.Equal("A", val.GetType().GetProperty("Building").GetValue(val));
        }

        [Fact]
        public async Task GetClassroomById_ReturnsNotFound_WhenClassroomNotExists()
        {
            // Act
            var result = await _controller.GetClassroomById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetClassroomById_ReturnsNotFound_WhenClassroomIsDeleted()
        {
            // Arrange
            _context.Classrooms.Add(new Classroom { Id = 5, Building = "D", RoomNumber = "Deleted", IsDeleted = true });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetClassroomById(5);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
