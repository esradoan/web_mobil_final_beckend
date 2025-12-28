using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query;

namespace SmartCampus.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusDbContext(options);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            _mockMapper = new Mock<IMapper>();

            _service = new UserService(_mockUserManager.Object, _mockMapper.Object, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnStudent_WhenRoleIsStudent()
        {
            // Arrange
            var user = new User { Id = 1, Email = "student@test.com" };
            _context.Users.Add(user);
            _context.Students.Add(new Student { Id = 10, UserId = 1, DepartmentId = 5 });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Student" });
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Id = 1 });

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(UserRole.Student, result.Role);
            Assert.Equal(5, result.DepartmentId);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldFallbackToStudent_WhenNoRole()
        {
            // Arrange
            var user = new User { Id = 2 };
            _context.Users.Add(user);
            _context.Students.Add(new Student { Id = 11, UserId = 2, DepartmentId = 6 });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(false);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>()); // No roles
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Id = 2 });

            // Act
            var result = await _service.GetProfileAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(UserRole.Student, result.Role);
            Assert.Equal(6, result.DepartmentId);
        }
        
        [Fact]
        public async Task GetProfileAsync_ShouldReturnFaculty_WhenRoleIsFaculty()
        {
             // Arrange
            var user = new User { Id = 3 };
            _context.Users.Add(user);
            _context.Faculties.Add(new Faculty { Id = 20, UserId = 3, DepartmentId = 7 });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(It.IsAny<User>())).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Faculty" });
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Id = 3 });

            // Act
            var result = await _service.GetProfileAsync(3);

            // Assert
            Assert.Equal(UserRole.Faculty, result.Role);
            Assert.Equal(7, result.DepartmentId);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateSelf()
        {
            // Arrange
            var user = new User { Id = 4, FirstName = "Old" };
            _mockUserManager.Setup(m => m.FindByIdAsync("4")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new UpdateUserDto { FirstName = "New" };

            // Act
            await _service.UpdateProfileAsync(4, dto);

            // Assert
            Assert.Equal("New", user.FirstName);
            _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_AdminShouldUpdateEmail()
        {
             // Arrange
            var adminUser = new User { Id = 5, Email = "admin@test.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(adminUser);
            _mockUserManager.Setup(m => m.IsInRoleAsync(adminUser, "Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.UpdateAsync(adminUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.FindByEmailAsync("new@test.com")).ReturnsAsync((User)null!);

            var dto = new UpdateUserDto { Email = "new@test.com" };

            // Act
            await _service.UpdateProfileAsync(5, dto);

            // Assert
            Assert.Equal("new@test.com", adminUser.Email);
            Assert.Equal("NEW@TEST.COM", adminUser.NormalizedEmail);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnUsers()
        {
            // Arrange
            // GetAllUsersAsync uses _userManager.Users which is IQueryable.
            // Mocking IQueryable on UserManager is hard because Users is a property.
            // However, _userManager.Users usually returns value from Mock Store or we can setup getter.
            // But standard Mock<UserManager> doesn't easily support IQueryable Users unless we setup the getter.
            
            // To make this work with Mock<UserManager>, we need to mock the `Users` property.
            var users = new List<User> 
            { 
                new User { Id = 1, Email = "u1" }, 
                new User { Id = 2, Email = "u2" } 
            };

            var usersQuery = new TestAsyncEnumerable<User>(users);

            _mockUserManager.Setup(u => u.Users).Returns(usersQuery);
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns((User u) => new UserDto { Id = u.Id });
            _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>());

            // Act
            var result = await _service.GetAllUsersAsync(1, 10);

            // Assert
            Assert.Equal(2, result.Count());
        }
        
        [Fact]
        public async Task UpdateProfilePictureAsync_ShouldUpdateUrl()
        {
            var user = new User { Id = 6 };
            _mockUserManager.Setup(m => m.FindByIdAsync("6")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _service.UpdateProfilePictureAsync(6, "http://img.com");
            
            Assert.Equal("http://img.com", user.ProfilePictureUrl);
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                     name: nameof(IQueryProvider.Execute),
                                     genericParameterCount: 1,
                                     types: new[] { typeof(Expression) })
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public T Current
        {
            get
            {
                return _inner.Current;
            }
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}
