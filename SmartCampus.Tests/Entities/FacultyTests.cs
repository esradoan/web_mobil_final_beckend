using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.Entities
{
    public class FacultyTests
    {
        [Fact]
        public void Faculty_Should_Have_Default_Values()
        {
            // Arrange & Act
            var faculty = new Faculty();

            // Assert
            Assert.Equal(string.Empty, faculty.EmployeeNumber);
            Assert.Equal(string.Empty, faculty.Title);
            Assert.Null(faculty.User); // Should be null by default if not set
            Assert.Null(faculty.Department);
            Assert.False(faculty.IsDeleted);
            Assert.Equal(0, faculty.UserId);
            Assert.Equal(0, faculty.DepartmentId);
        }

        [Fact]
        public void Faculty_Should_Set_Properties_Correctly()
        {
            // Arrange
            var user = new User { UserName = "prof1" };
            var department = new Department { Name = "Math" };
            
            // Act
            var faculty = new Faculty
            {
                UserId = 10,
                User = user,
                DepartmentId = 5,
                Department = department,
                EmployeeNumber = "EMP001",
                Title = "Professor",
                Id = 1
            };

            // Assert
            Assert.Equal(10, faculty.UserId);
            Assert.Same(user, faculty.User);
            Assert.Equal(5, faculty.DepartmentId);
            Assert.Same(department, faculty.Department);
            Assert.Equal("EMP001", faculty.EmployeeNumber);
            Assert.Equal("Professor", faculty.Title);
            Assert.Equal(1, faculty.Id);
        }

        [Fact]
        public void Faculty_Should_Inherit_BaseEntity_Properties()
        {
            // Arrange
            var faculty = new Faculty();
            var now = DateTime.UtcNow;

            // Act
            faculty.CreatedAt = now;
            faculty.UpdatedAt = now.AddMinutes(5);
            faculty.IsDeleted = true;

            // Assert
            Assert.Equal(now, faculty.CreatedAt);
            Assert.Equal(now.AddMinutes(5), faculty.UpdatedAt);
            Assert.True(faculty.IsDeleted);
        }
    }
}
