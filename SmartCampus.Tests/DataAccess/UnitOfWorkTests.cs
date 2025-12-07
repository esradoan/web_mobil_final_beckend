using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.DataAccess
{
    public class UnitOfWorkTests
    {
        private CampusDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new CampusDbContext(options);
        }

        [Fact]
        public async Task CompleteAsync_Should_Persist_Changes()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var unitOfWork = new UnitOfWork(context);
            var repo = unitOfWork.Repository<Department>();

            var department = new Department { Name = "Test Dept" };

            // Act
            await repo.AddAsync(department);
            var result = await unitOfWork.CompleteAsync();

            // Assert
            Assert.True(result > 0);
            var saved = await context.Departments.FirstOrDefaultAsync();
            Assert.NotNull(saved);
        }

        [Fact]
        public void Repository_Should_Return_Same_Instance_For_Same_Type()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var unitOfWork = new UnitOfWork(context);

            // Act
            var repo1 = unitOfWork.Repository<Department>();
            var repo2 = unitOfWork.Repository<Department>();

            // Assert
            Assert.Same(repo1, repo2);
        }

        [Fact]
        public void Repository_Should_Return_Different_Instances_For_Different_Types()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var unitOfWork = new UnitOfWork(context);

            // Act
            var deptRepo = unitOfWork.Repository<Department>();
            var studentRepo = unitOfWork.Repository<Student>();

            // Assert
            Assert.NotSame(deptRepo, studentRepo);
        }
    }
}
