using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class MealsControllerTests
    {
        private readonly Mock<IMealService> _mockService;
        private readonly MealsController _controller;

        public MealsControllerTests()
        {
            _mockService = new Mock<IMealService>();
            _controller = new MealsController(_mockService.Object);
            SetupUserContext(_controller, userId: 1001, role: "Student");
        }

        private void SetupUserContext(ControllerBase controller, int userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetCafeterias_ShouldReturnOk_WithCafeterias()
        {
            // Arrange
            var expected = new List<CafeteriaDto> { new CafeteriaDto { Id = 1, Name = "Main Cafeteria" } };
            _mockService.Setup(s => s.GetCafeteriasAsync()).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetCafeterias();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMenus_ShouldReturnOk_WithMenus()
        {
            // Arrange
            var expected = new List<MealMenuDto> { new MealMenuDto { Id = 1, MealType = "lunch" } };
            _mockService.Setup(s => s.GetMenusAsync(null, null)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetMenus(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMenu_ShouldReturnOk_WhenMenuExists()
        {
            // Arrange
            var expected = new MealMenuDto { Id = 1, MealType = "lunch" };
            _mockService.Setup(s => s.GetMenuByIdAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetMenu(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetMenu_ShouldReturnNotFound_WhenMenuDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetMenuByIdAsync(999)).ReturnsAsync((MealMenuDto?)null);

            // Act
            var result = await _controller.GetMenu(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateMenu_ShouldReturnCreated_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new CreateMealMenuDto { CafeteriaId = 1, MealType = "lunch" };
            var expected = new MealMenuDto { Id = 1, MealType = "lunch" };
            _mockService.Setup(s => s.CreateMenuAsync(dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateMenu(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, createdResult.Value);
        }

        [Fact]
        public async Task CreateMenu_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new CreateMealMenuDto { CafeteriaId = 1 };
            _mockService.Setup(s => s.CreateMenuAsync(dto)).ThrowsAsync(new Exception("Invalid data"));

            // Act
            var result = await _controller.CreateMenu(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateMenu_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UpdateMealMenuDto { Items = new List<string> { "Soup", "Salad" } };
            var expected = new MealMenuDto { Id = 1, Items = new List<string> { "Soup", "Salad" } };
            _mockService.Setup(s => s.UpdateMenuAsync(1, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.UpdateMenu(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task UpdateMenu_ShouldReturnNotFound_WhenMenuDoesNotExist()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UpdateMealMenuDto { Items = new List<string> { "Pizza" } };
            _mockService.Setup(s => s.UpdateMenuAsync(999, dto)).ReturnsAsync((MealMenuDto?)null);

            // Act
            var result = await _controller.UpdateMenu(999, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteMenu_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            _mockService.Setup(s => s.DeleteMenuAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMenu(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteMenu_ShouldReturnNotFound_WhenMenuDoesNotExist()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            _mockService.Setup(s => s.DeleteMenuAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteMenu(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateReservation_ShouldReturnCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateMealReservationDto { MenuId = 1 };
            var expected = new MealReservationDto { Id = 1, MenuId = 1 };
            _mockService.Setup(s => s.CreateReservationAsync(1001, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateReservation(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, createdResult.Value);
        }

        [Fact]
        public async Task CreateReservation_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var dto = new CreateMealReservationDto { MenuId = 1 };
            _mockService.Setup(s => s.CreateReservationAsync(1001, dto)).ThrowsAsync(new Exception("Limit exceeded"));

            // Act
            var result = await _controller.CreateReservation(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelReservation_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.CancelReservationAsync(1001, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.CancelReservation(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CancelReservation_ShouldReturnNotFound_WhenReservationDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.CancelReservationAsync(1001, 999)).ReturnsAsync(false);

            // Act
            var result = await _controller.CancelReservation(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyReservations_ShouldReturnOk_WithReservations()
        {
            // Arrange
            var expected = new List<MealReservationDto> { new MealReservationDto { Id = 1 } };
            _mockService.Setup(s => s.GetMyReservationsAsync(1001)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetMyReservations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ValidateReservation_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UseReservationDto { QrCode = "ABC123" };
            var expected = new MealReservationDto { Id = 1, QrCode = "ABC123" };
            _mockService.Setup(s => s.ValidateReservationByQrCodeAsync("ABC123")).ReturnsAsync(expected);

            // Act
            var result = await _controller.ValidateReservation(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ValidateReservation_ShouldReturnNotFound_WhenReservationNotFound()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UseReservationDto { QrCode = "INVALID" };
            _mockService.Setup(s => s.ValidateReservationByQrCodeAsync("INVALID")).ReturnsAsync((MealReservationDto?)null);

            // Act
            var result = await _controller.ValidateReservation(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UseReservation_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UseReservationDto { QrCode = "ABC123" };
            var expected = new MealReservationDto { Id = 1, QrCode = "ABC123", Status = "used" };
            _mockService.Setup(s => s.UseReservationAsync("ABC123")).ReturnsAsync(expected);

            // Act
            var result = await _controller.UseReservation(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UseReservation_ShouldReturnNotFound_WhenReservationNotFound()
        {
            // Arrange
            SetupUserContext(_controller, userId: 1001, role: "Admin");
            var dto = new UseReservationDto { QrCode = "INVALID" };
            _mockService.Setup(s => s.UseReservationAsync("INVALID")).ReturnsAsync((MealReservationDto?)null);

            // Act
            var result = await _controller.UseReservation(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
