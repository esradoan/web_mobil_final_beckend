using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class MealServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly MealService _mealService;

        public MealServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _mealService = new MealService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== MENU TESTS ====================

        [Fact]
        public async Task CreateMenuAsync_ShouldCreateMenu_WhenValidDto()
        {
            // Arrange
            var cafeteria = new Cafeteria { Id = 1, Name = "Main", Capacity = 100, IsActive = true };
            _context.Cafeterias.Add(cafeteria);
            await _context.SaveChangesAsync();

            var dto = new CreateMealMenuDto
            {
                CafeteriaId = 1,
                Date = DateTime.Today.AddDays(1),
                MealType = "lunch",
                Price = 50,
                IsPublished = true,
                Items = new List<string> { "Soup", "Dish" },
                Nutrition = new NutritionInfo { Calories = 500 }
            };

            // Act
            var result = await _mealService.CreateMenuAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Price);
            Assert.Equal(500, result.Nutrition?.Calories);
            
            var dbMenu = await _context.MealMenus.FindAsync(result.Id);
            Assert.NotNull(dbMenu);
        }

        [Fact]
        public async Task UpdateMenuAsync_ShouldUpdate_WhenExists()
        {
            // Arrange
            var cafeteria = new Cafeteria { Id = 1, Name = "Main" };
            _context.Cafeterias.Add(cafeteria);
            var menu = new MealMenu { Id = 1, CafeteriaId = 1, Date = DateTime.Today, MealType = "lunch", Price = 50, ItemsJson = "[]", NutritionJson = "{}", IsPublished = true, CreatedAt = DateTime.UtcNow };
            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateMealMenuDto { Price = 60, IsPublished = false };

            // Act
            var result = await _mealService.UpdateMenuAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(60, result.Price);
            Assert.False(result.IsPublished);
        }

        [Fact]
        public async Task DeleteMenuAsync_ShouldDelete_WhenExists()
        {
            // Arrange
            var menu = new MealMenu { Id = 1, CafeteriaId = 1, Date = DateTime.Today, MealType = "lunch", ItemsJson="[]", NutritionJson="{}", CreatedAt = DateTime.UtcNow };
            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.DeleteMenuAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.MealMenus.FindAsync(1));
        }

        // ==================== RESERVATION TESTS ====================

        [Fact]
        public async Task CreateReservationAsync_ShouldCreate_WithWalletBalance()
        {
            // Arrange
            var userId = 101;
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100 };
            
            var cafeteria = new Cafeteria { Id = 1, Name = "Main" };
            var menu = new MealMenu { Id = 10, CafeteriaId = 1, Date = DateTime.Today.AddDays(1), MealType = "lunch", Price = 40, IsPublished = true, ItemsJson="[]", NutritionJson="{}", CreatedAt = DateTime.UtcNow };

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.Cafeterias.Add(cafeteria);
            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            var dto = new CreateMealReservationDto { MenuId = 10 };

            // Act
            var result = await _mealService.CreateReservationAsync(userId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("reserved", result.Status);
            Assert.NotNull(result.QrCode);
            
            // Verify wallet deduction
            var dbWallet = await _context.Wallets.FindAsync(1);
            Assert.Equal(60, dbWallet.Balance); // 100 - 40

            // Verify transaction
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.ReferenceId == result.Id && t.Type == "debit");
            Assert.NotNull(transaction);
            Assert.Equal(40, transaction.Amount);
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldFail_WhenInsufficientBalance()
        {
            // Arrange
            var userId = 102;
            var user = new User { Id = userId };
            var wallet = new Wallet { Id = 2, UserId = userId, Balance = 10 }; // Only 10
            
            var cafeteria = new Cafeteria { Id = 1, Name = "Main" };
            var menu = new MealMenu { Id = 10, CafeteriaId = 1, Date = DateTime.Today.AddDays(1), Price = 40, IsPublished = true, MealType="lunch", ItemsJson="[]", NutritionJson="{}", CreatedAt = DateTime.UtcNow };

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.Cafeterias.Add(cafeteria);
            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _mealService.CreateReservationAsync(userId, new CreateMealReservationDto { MenuId = 10 }));
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldCreate_ForScholarshipStudent()
        {
            // Arrange
            var userId = 103;
            var user = new User { Id = userId };
            var student = new Student { Id = 1, UserId = userId, IsScholarship = true };
            // No wallet needed for scholarship (or empty wallet)
            
            var cafeteria = new Cafeteria { Id = 1, Name = "Main" };
            var menu = new MealMenu { Id = 10, CafeteriaId = 1, Date = DateTime.Today.AddDays(1), Price = 40, IsPublished = true, MealType="lunch", ItemsJson="[]", NutritionJson="{}", CreatedAt = DateTime.UtcNow };

            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.Cafeterias.Add(cafeteria);
            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.CreateReservationAsync(userId, new CreateMealReservationDto { MenuId = 10 });

            // Assert
            Assert.True(result.IsScholarship);
            Assert.Equal("reserved", result.Status);
        }

        [Fact]
        public async Task CancelReservationAsync_ShouldRefund_WhenPaid()
        {
            // Arrange
            var userId = 101;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 60 };
            var menu = new MealMenu { Id = 10, Date = DateTime.Today.AddDays(2), MealType = "lunch", ItemsJson="[]", NutritionJson="{}", CreatedAt = DateTime.UtcNow };
            var reservation = new MealReservation 
            { 
                Id = 500, 
                UserId = userId, 
                MenuId = 10, 
                Amount = 40, 
                Status = "reserved", 
                IsScholarship = false,
                Date = menu.Date,
                MealType = "lunch",
                QrCode = "QR",
                CreatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            _context.MealMenus.Add(menu);
            _context.MealReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.CancelReservationAsync(userId, 500);

            // Assert
            Assert.True(result);
            var dbReservation = await _context.MealReservations.FindAsync(500);
            Assert.Equal("cancelled", dbReservation.Status);

            var dbWallet = await _context.Wallets.FindAsync(1);
            Assert.Equal(100, dbWallet.Balance); // 60 + 40 refunded
        }

        [Fact]
        public async Task ValidateReservationByQrCodeAsync_ShouldReturnReservation()
        {
            // Arrange
            var reservation = new MealReservation 
            { 
                Id = 600, 
                QrCode = "VALID-QR", 
                Status = "reserved", 
                Date = DateTime.Today.AddDays(1), 
                MealType = "lunch",
                CreatedAt = DateTime.UtcNow,
                UserId = 1,
                MenuId = 1
            };
            
            // Add related entities to avoid include errors
            var user = new User { Id = 1, FirstName="A", LastName="B" };
            var menu = new MealMenu { Id = 1, ItemsJson="[]", NutritionJson="{}" };
            var cafeteria = new Cafeteria { Id = 1, Name="Caf" };
            
            reservation.User = user;
            reservation.Menu = menu;
            reservation.Cafeteria = cafeteria; // Manually linking for test setup sometimes needed

            _context.Users.Add(user);
            _context.MealMenus.Add(menu);
            _context.Cafeterias.Add(cafeteria);
            _context.MealReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.ValidateReservationByQrCodeAsync("VALID-QR");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(600, result.Id);
        }

        [Fact]
        public async Task UseReservationAsync_ShouldMarkAsUsed()
        {
            // Arrange
            var reservation = new MealReservation 
            { 
                Id = 700, 
                QrCode = "USE-QR", 
                Status = "reserved", 
                Date = DateTime.Today.AddDays(1), 
                MealType="lunch",
                CreatedAt = DateTime.UtcNow,
                UserId = 1,
                MenuId = 1,
                Amount = 10
            };
            
            var user = new User { Id = 1, FirstName="A", LastName="B" };
            var menu = new MealMenu { Id = 1, ItemsJson="[]", NutritionJson="{}" };
            var cafeteria = new Cafeteria { Id = 1, Name="Caf" };
            
            reservation.User = user;
            reservation.Menu = menu;
            reservation.Cafeteria = cafeteria;
            
            // To test backward compatibility (transaction check), we verify it DOES NOT deduct if already deducted (which is default now)
            // But if we want to test that path, we would need to prevent transaction creation in setup?
            // Actually, the service creates transaction at reservation time. So usually transaction exists.
            
            _context.Users.Add(user);
            _context.MealMenus.Add(menu);
            _context.Cafeterias.Add(cafeteria);
            _context.MealReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.UseReservationAsync("USE-QR");

            // Assert
            Assert.Equal("used", result.Status);
            Assert.NotNull(result.UsedAt);
            
            var dbRes = await _context.MealReservations.FindAsync(700);
            Assert.Equal("used", dbRes.Status);
        }
        
        [Fact]
        public async Task GetCafeteriasAsync_ShouldReturnActive()
        {
            // Arrange
            _context.Cafeterias.Add(new Cafeteria { Id = 1, Name = "A", IsActive = true });
            _context.Cafeterias.Add(new Cafeteria { Id = 2, Name = "B", IsActive = false });
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.GetCafeteriasAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("A", result[0].Name);
        }
    }
}
