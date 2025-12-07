using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.DataAccess
{
    public class GenericRepositoryTests
    {
        private CampusDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new CampusDbContext(options);
        }

        [Fact]
        public async Task AddAsync_Should_Add_Entity_To_Database()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            var department = new Department { Name = "Physics" };

            // Act
            await repository.AddAsync(department);
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.Departments.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Physics", saved.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Entity()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            var department = new Department { Name = "Chemistry" };
            await context.Departments.AddAsync(department);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(department.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(department.Id, result.Id);
            Assert.Equal("Chemistry", result.Name);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_All_Entities()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            await context.Departments.AddRangeAsync(
                new Department { Name = "Math" },
                new Department { Name = "Biology" }
            );
            await context.SaveChangesAsync();

            // Act
            var results = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task Update_Should_Modify_Entity_And_Set_UpdatedAt()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            var department = new Department { Name = "Old Name" };
            await context.Departments.AddAsync(department);
            await context.SaveChangesAsync();

            // Act
            department.Name = "New Name";
            repository.Update(department); // Should set updated state + UpdatedAt
            await context.SaveChangesAsync();

            // Assert
            var updated = await context.Departments.FirstAsync();
            Assert.Equal("New Name", updated.Name);
            Assert.NotNull(updated.UpdatedAt);
            Assert.True(updated.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task Delete_Should_Remove_Entity()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            var department = new Department { Name = "To Delete" };
            await context.Departments.AddAsync(department);
            await context.SaveChangesAsync();

            // Act
            repository.Delete(department);
            await context.SaveChangesAsync();

            // Assert
            var deleted = await context.Departments.FirstOrDefaultAsync();
            Assert.Null(deleted);
        }

        [Fact]
        public async Task FindAsync_Should_Return_Matching_Entities()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new GenericRepository<Department>(context);
            await context.Departments.AddRangeAsync(
                new Department { Name = "FindMe" },
                new Department { Name = "FindMeNot" }
            );
            await context.SaveChangesAsync();

            // Act
            var results = await repository.FindAsync(d => d.Name == "FindMe");

            // Assert
            Assert.Single(results);
            Assert.Equal("FindMe", results.First().Name);
        }
    }
}
