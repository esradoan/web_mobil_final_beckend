using Microsoft.EntityFrameworkCore.Migrations;
using SmartCampus.DataAccess.Migrations;
using Xunit;

namespace SmartCampus.Tests.Migrations
{
    public class AddIdentityMigrationTests
    {
        [Fact]
        public void Migration_Should_Be_Valid_Class()
        {
            // Arrange
            var migration = new AddIdentity();

            // Assert
            Assert.NotNull(migration);
            Assert.IsAssignableFrom<Migration>(migration);
        }

        [Fact]
        public void Migration_Should_Have_Up_And_Down_Methods()
        {
            // Arrange
            var migrationType = typeof(AddIdentity);
            var upMethod = migrationType.GetMethod("Up", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var downMethod = migrationType.GetMethod("Down", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Assert
            Assert.NotNull(upMethod);
            Assert.NotNull(downMethod);
        }
    }
}
