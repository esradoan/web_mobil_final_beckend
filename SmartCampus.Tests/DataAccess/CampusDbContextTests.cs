using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.DataAccess
{
    public class CampusDbContextTests
    {
        private CampusDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            return new CampusDbContext(options);
        }

        [Fact]
        public async Task Can_Add_And_Retrieve_Department()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var department = new Department
            {
                Name = "Computer Science"
            };

            // Act
            await context.Departments.AddAsync(department);
            await context.SaveChangesAsync();

            // Assert
            var savedDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Computer Science");
            Assert.NotNull(savedDepartment);
            Assert.Equal("Computer Science", savedDepartment.Name);
            Assert.True(savedDepartment.Id > 0);
        }

        [Fact]
        public async Task Can_Add_User_Identity()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new User
            {
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Assert
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "testuser@example.com");
            Assert.NotNull(savedUser);
            Assert.Equal("Test", savedUser.FirstName);
        }

        [Fact]
        public async Task Should_Enforce_Student_Department_Relationship()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            var department = new Department { Name = "Engineering" };
            await context.Departments.AddAsync(department);
            await context.SaveChangesAsync();

            var user = new User { UserName = "student1", Email = "s1@test.com", FirstName = "S", LastName = "1" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.Id,
                DepartmentId = department.Id,
                StudentNumber = "12345"
            };

            // Act
            await context.Students.AddAsync(student);
            await context.SaveChangesAsync();

            // Assert
            var savedStudent = await context.Students
                .Include(s => s.Department)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentNumber == "12345");

            Assert.NotNull(savedStudent);
            Assert.Equal("Engineering", savedStudent.Department.Name);
            Assert.Equal("s1@test.com", savedStudent.User.Email);
        }
    }
}
