#nullable disable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Entities;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System;

namespace SmartCampus.Tests.Controllers
{
    public class AdminSetupControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<ILogger<AdminSetupController>> _mockLogger;
        private readonly AdminSetupController _controller;

        public AdminSetupControllerTests()
        {
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);
            
            var roleStore = new Mock<IRoleStore<Role>>();
            _mockRoleManager = new Mock<RoleManager<Role>>(roleStore.Object, null, null, null, null);
            
            _mockLogger = new Mock<ILogger<AdminSetupController>>();
            
            _controller = new AdminSetupController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object);
        }

        // CheckAdmin Tests
        [Fact]
        public async Task CheckAdmin_ReturnsOk_WhenAdminNotExists()
        {
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var result = await _controller.CheckAdmin();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CheckAdmin_ReturnsOk_WithAdminDetails_WhenAdminExists()
        {
            var adminUser = new User { Id = 1, Email = "admin@smartcampus.edu", FirstName = "Admin", LastName = "User", EmailConfirmed = true };
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(adminUser);
            _mockUserManager.Setup(x => x.IsInRoleAsync(adminUser, "Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(adminUser)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.IsLockedOutAsync(adminUser)).ReturnsAsync(false);

            var result = await _controller.CheckAdmin();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        // CreateAdmin Tests
        [Fact]
        public async Task CreateAdmin_ReturnsBadRequest_WhenAdminAlreadyExists()
        {
            var existingAdmin = new User { Id = 1, Email = "admin@test.com" };
            var dto = new CreateAdminDto { Email = "admin@test.com", Password = "Test123!" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(existingAdmin);
            _mockUserManager.Setup(x => x.IsInRoleAsync(existingAdmin, "Admin")).ReturnsAsync(true);

            var result = await _controller.CreateAdmin(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAdmin_ReturnsOk_WhenNewAdminCreated()
        {
            var dto = new CreateAdminDto 
            { 
                Email = "newadmin@test.com", 
                Password = "Test123!",
                FirstName = "New",
                LastName = "Admin"
            };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User)null);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.CreateAdmin(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateAdmin_ReturnsBadRequest_WhenCreateFails()
        {
            var dto = new CreateAdminDto { Email = "admin@test.com", Password = "weak" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User)null);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var result = await _controller.CreateAdmin(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAdmin_CreatesRoleIfNotExists()
        {
            var dto = new CreateAdminDto { Email = "admin@test.com", Password = "Test123!" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User)null);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(false);
            _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<Role>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.CreateAdmin(dto);

            _mockRoleManager.Verify(x => x.CreateAsync(It.Is<Role>(r => r.Name == "Admin")), Times.Once);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateAdmin_AddsRoleToExistingUser_WhenUserExistsButNotAdmin()
        {
            var existingUser = new User { Id = 1, Email = "user@test.com" };
            var dto = new CreateAdminDto { Email = "user@test.com", Password = "Test123!" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(x => x.IsInRoleAsync(existingUser, "Admin")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.AddToRoleAsync(existingUser, "Admin")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(existingUser)).ReturnsAsync("reset-token");
            _mockUserManager.Setup(x => x.ResetPasswordAsync(existingUser, "reset-token", dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.CreateAdmin(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
