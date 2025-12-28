using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
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
            _context.Database.EnsureCreated();
            _mealService = new MealService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateMenuAsync_ShouldCreateMenu_WhenValidDto()
        {
            // Arrange
            var cafeteria = new Cafeteria { Id = 1001, Name = "Main Cafeteria", Capacity = 100, IsActive = true };
            _context.Cafeterias.Add(cafeteria);
            await _context.SaveChangesAsync();

            var dto = new CreateMealMenuDto
            {
                CafeteriaId = 1001,
                Date = DateTime.UtcNow.Date,
                MealType = "lunch",
                Price = 50,
                IsPublished = true,
                Items = new List<string> { "Soup", "Main Dish", "Salad" }
            };

            // Act
            var result = await _mealService.CreateMenuAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1001, result.CafeteriaId);
            Assert.Equal("lunch", result.MealType);
            Assert.Equal(3, result.Items.Count);

            var dbMenu = await _context.MealMenus.FindAsync(result.Id);
            Assert.NotNull(dbMenu);
        }

        [Fact]
        public async Task GetMenusAsync_ShouldReturnPublishedMenus()
        {
            // Arrange
            var cafeteria = new Cafeteria { Id = 1001, Name = "Main Cafeteria" };
            _context.Cafeterias.Add(cafeteria);
            
            _context.MealMenus.Add(new MealMenu 
            { 
                CafeteriaId = 1001, 
                Date = DateTime.UtcNow.Date,  
                MealType = "lunch", 
                IsPublished = true,
                Price = 50,
                ItemsJson = "[\"Soup\"]",
                NutritionJson = "{}",
                CreatedAt = DateTime.UtcNow
            });
            
            _context.MealMenus.Add(new MealMenu 
            { 
                CafeteriaId = 1, 
                Date = DateTime.UtcNow.Date.AddDays(1), 
                MealType = "dinner", 
                IsPublished = false, // Not published
                Price = 60,
                ItemsJson = "[]",
                NutritionJson = "{}",
                CreatedAt = DateTime.UtcNow
            });
            
            await _context.SaveChangesAsync();

            // Act
            var result = await _mealService.GetMenusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the published one
            Assert.Equal("lunch", result[0].MealType);
        }
    }
}
