using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SmartCampus.Business.Services
{
    public interface IMealService
    {
        // Menu Operations
        Task<List<MealMenuDto>> GetMenusAsync(DateTime? date = null, int? cafeteriaId = null);
        Task<MealMenuDto?> GetMenuByIdAsync(int id);
        Task<MealMenuDto> CreateMenuAsync(CreateMealMenuDto dto);
        Task<MealMenuDto?> UpdateMenuAsync(int id, UpdateMealMenuDto dto);
        Task<bool> DeleteMenuAsync(int id);
        
        // Reservation Operations
        Task<MealReservationDto> CreateReservationAsync(int userId, CreateMealReservationDto dto);
        Task<bool> CancelReservationAsync(int userId, int reservationId);
        Task<List<MealReservationDto>> GetMyReservationsAsync(int userId);
        Task<MealReservationDto?> UseReservationAsync(string qrCode);
        
        // Cafeteria Operations
        Task<List<CafeteriaDto>> GetCafeteriasAsync();
    }

    // ==================== DTOs ====================

    public class MealMenuDto
    {
        public int Id { get; set; }
        public int CafeteriaId { get; set; }
        public string CafeteriaName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string MealType { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public NutritionInfo? Nutrition { get; set; }
        public bool IsPublished { get; set; }
        public bool HasVegetarianOption { get; set; }
        public decimal Price { get; set; }
    }

    public class NutritionInfo
    {
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Carbs { get; set; }
        public int Fat { get; set; }
    }

    public class CreateMealMenuDto
    {
        public int CafeteriaId { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; } = "lunch";
        public List<string> Items { get; set; } = new();
        public NutritionInfo? Nutrition { get; set; }
        public bool HasVegetarianOption { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; } = true;
    }

    public class UpdateMealMenuDto
    {
        public List<string>? Items { get; set; }
        public NutritionInfo? Nutrition { get; set; }
        public bool? HasVegetarianOption { get; set; }
        public decimal? Price { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class MealReservationDto
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public string CafeteriaName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string MealType { get; set; } = string.Empty;
        public List<string> MenuItems { get; set; } = new();
        public decimal Amount { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? UsedAt { get; set; }
        public bool IsScholarship { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMealReservationDto
    {
        public int MenuId { get; set; }
    }

    public class CafeteriaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }

    // ==================== SERVICE ====================

    public class MealService : IMealService
    {
        private readonly CampusDbContext _context;
        private const int MAX_DAILY_SCHOLARSHIP_MEALS = 2;

        public MealService(CampusDbContext context)
        {
            _context = context;
        }

        // ==================== MENU OPERATIONS ====================

        public async Task<List<MealMenuDto>> GetMenusAsync(DateTime? date = null, int? cafeteriaId = null)
        {
            var query = _context.MealMenus
                .Include(m => m.Cafeteria)
                .Where(m => m.IsPublished);

            if (date.HasValue)
            {
                query = query.Where(m => m.Date.Date == date.Value.Date);
            }

            if (cafeteriaId.HasValue)
            {
                query = query.Where(m => m.CafeteriaId == cafeteriaId.Value);
            }

            var menus = await query.OrderBy(m => m.Date).ThenBy(m => m.MealType).ToListAsync();

            return menus.Select(MapToMenuDto).ToList();
        }

        public async Task<MealMenuDto?> GetMenuByIdAsync(int id)
        {
            var menu = await _context.MealMenus
                .Include(m => m.Cafeteria)
                .FirstOrDefaultAsync(m => m.Id == id);

            return menu == null ? null : MapToMenuDto(menu);
        }

        public async Task<MealMenuDto> CreateMenuAsync(CreateMealMenuDto dto)
        {
            var cafeteria = await _context.Cafeterias.FindAsync(dto.CafeteriaId);
            if (cafeteria == null)
                throw new Exception("Cafeteria not found");

            var menu = new MealMenu
            {
                CafeteriaId = dto.CafeteriaId,
                Date = dto.Date.Date,
                MealType = dto.MealType,
                ItemsJson = JsonSerializer.Serialize(dto.Items),
                NutritionJson = dto.Nutrition != null ? JsonSerializer.Serialize(dto.Nutrition) : "{}",
                HasVegetarianOption = dto.HasVegetarianOption,
                Price = dto.Price,
                IsPublished = dto.IsPublished,
                CreatedAt = DateTime.UtcNow
            };

            _context.MealMenus.Add(menu);
            await _context.SaveChangesAsync();

            menu.Cafeteria = cafeteria;
            return MapToMenuDto(menu);
        }

        public async Task<MealMenuDto?> UpdateMenuAsync(int id, UpdateMealMenuDto dto)
        {
            var menu = await _context.MealMenus
                .Include(m => m.Cafeteria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null) return null;

            if (dto.Items != null)
                menu.ItemsJson = JsonSerializer.Serialize(dto.Items);
            if (dto.Nutrition != null)
                menu.NutritionJson = JsonSerializer.Serialize(dto.Nutrition);
            if (dto.HasVegetarianOption.HasValue)
                menu.HasVegetarianOption = dto.HasVegetarianOption.Value;
            if (dto.Price.HasValue)
                menu.Price = dto.Price.Value;
            if (dto.IsPublished.HasValue)
                menu.IsPublished = dto.IsPublished.Value;

            menu.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToMenuDto(menu);
        }

        public async Task<bool> DeleteMenuAsync(int id)
        {
            var menu = await _context.MealMenus.FindAsync(id);
            if (menu == null) return false;

            _context.MealMenus.Remove(menu);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== RESERVATION OPERATIONS ====================

        public async Task<MealReservationDto> CreateReservationAsync(int userId, CreateMealReservationDto dto)
        {
            var menu = await _context.MealMenus
                .Include(m => m.Cafeteria)
                .FirstOrDefaultAsync(m => m.Id == dto.MenuId);

            if (menu == null)
                throw new Exception("Menu not found");

            if (!menu.IsPublished)
                throw new Exception("Menu is not available for reservation");

            if (menu.Date.Date < DateTime.UtcNow.Date)
                throw new Exception("Cannot reserve for past dates");

            // Check if already reserved
            var existingReservation = await _context.MealReservations
                .AnyAsync(r => r.UserId == userId && r.MenuId == dto.MenuId && r.Status != "cancelled");

            if (existingReservation)
                throw new Exception("You already have a reservation for this meal");

            // Get user info
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            // Check if scholarship student
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            bool isScholarship = student?.IsScholarship ?? false;

            decimal amount = 0;

            if (isScholarship)
            {
                // Check daily quota for scholarship students
                var todayReservations = await _context.MealReservations
                    .CountAsync(r => r.UserId == userId && 
                                     r.Date.Date == menu.Date.Date && 
                                     r.IsScholarship &&
                                     r.Status != "cancelled");

                if (todayReservations >= MAX_DAILY_SCHOLARSHIP_MEALS)
                {
                    throw new Exception($"Daily quota exceeded. Scholarship students can have max {MAX_DAILY_SCHOLARSHIP_MEALS} meals per day.");
                }
            }
            else
            {
                // Check wallet balance for paid students
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.Balance < menu.Price)
                {
                    throw new Exception($"Insufficient balance. Required: {menu.Price} TRY");
                }
                amount = menu.Price;
            }

            // Create reservation
            var reservation = new MealReservation
            {
                UserId = userId,
                MenuId = dto.MenuId,
                CafeteriaId = menu.CafeteriaId,
                MealType = menu.MealType,
                Date = menu.Date,
                Amount = amount,
                QrCode = GenerateQrCode(),
                Status = "reserved",
                IsScholarship = isScholarship,
                CreatedAt = DateTime.UtcNow
            };

            _context.MealReservations.Add(reservation);
            await _context.SaveChangesAsync();

            reservation.Menu = menu;
            reservation.Cafeteria = menu.Cafeteria;

            return MapToReservationDto(reservation);
        }

        public async Task<bool> CancelReservationAsync(int userId, int reservationId)
        {
            var reservation = await _context.MealReservations
                .Include(r => r.Menu)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null)
                return false;

            if (reservation.Status != "reserved")
                throw new Exception("Only reserved meals can be cancelled");

            // Check if at least 2 hours before meal time
            var mealDateTime = reservation.Date.Date;
            if (reservation.MealType == "lunch")
                mealDateTime = mealDateTime.AddHours(12);
            else
                mealDateTime = mealDateTime.AddHours(18);

            if (DateTime.UtcNow > mealDateTime.AddHours(-2))
                throw new Exception("Cancellation must be at least 2 hours before meal time");

            // If paid, refund to wallet
            if (!reservation.IsScholarship && reservation.Amount > 0)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet != null)
                {
                    wallet.Balance += reservation.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // Create refund transaction
                    var transaction = new Transaction
                    {
                        WalletId = wallet.Id,
                        Type = "credit",
                        Amount = reservation.Amount,
                        BalanceAfter = wallet.Balance,
                        ReferenceType = "meal_refund",
                        ReferenceId = reservation.Id,
                        Description = $"Yemek rezervasyonu iptali - {reservation.Date:dd.MM.yyyy} {reservation.MealType}",
                        Status = "completed",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(transaction);
                }
            }

            reservation.Status = "cancelled";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<MealReservationDto>> GetMyReservationsAsync(int userId)
        {
            var reservations = await _context.MealReservations
                .Include(r => r.Menu)
                .Include(r => r.Cafeteria)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return reservations.Select(MapToReservationDto).ToList();
        }

        public async Task<MealReservationDto?> UseReservationAsync(string qrCode)
        {
            var reservation = await _context.MealReservations
                .Include(r => r.Menu)
                .Include(r => r.Cafeteria)
                .FirstOrDefaultAsync(r => r.QrCode == qrCode);

            if (reservation == null)
                throw new Exception("Invalid QR code");

            if (reservation.Status == "used")
                throw new Exception("This reservation has already been used");

            if (reservation.Status == "cancelled")
                throw new Exception("This reservation was cancelled");

            if (reservation.Date.Date != DateTime.UtcNow.Date)
                throw new Exception("This reservation is not valid for today");

            // Mark as used
            reservation.Status = "used";
            reservation.UsedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

            // If paid, deduct from wallet
            if (!reservation.IsScholarship && reservation.Amount > 0)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == reservation.UserId);
                if (wallet != null)
                {
                    wallet.Balance -= reservation.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // Create debit transaction
                    var transaction = new Transaction
                    {
                        WalletId = wallet.Id,
                        Type = "debit",
                        Amount = reservation.Amount,
                        BalanceAfter = wallet.Balance,
                        ReferenceType = "meal",
                        ReferenceId = reservation.Id,
                        Description = $"Yemek ödemesi - {reservation.Date:dd.MM.yyyy} {reservation.MealType}",
                        Status = "completed",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();

            return MapToReservationDto(reservation);
        }

        // ==================== CAFETERIA OPERATIONS ====================

        public async Task<List<CafeteriaDto>> GetCafeteriasAsync()
        {
            var cafeterias = await _context.Cafeterias
                .Where(c => c.IsActive)
                .ToListAsync();

            return cafeterias.Select(c => new CafeteriaDto
            {
                Id = c.Id,
                Name = c.Name,
                Location = c.Location,
                Capacity = c.Capacity,
                IsActive = c.IsActive
            }).ToList();
        }

        // ==================== HELPER METHODS ====================

        private string GenerateQrCode()
        {
            return $"MEAL-{Guid.NewGuid():N}"[..20].ToUpper();
        }

        private MealMenuDto MapToMenuDto(MealMenu menu)
        {
            var items = new List<string>();
            var nutrition = new NutritionInfo();

            try
            {
                items = JsonSerializer.Deserialize<List<string>>(menu.ItemsJson) ?? new List<string>();
            }
            catch { }

            try
            {
                nutrition = JsonSerializer.Deserialize<NutritionInfo>(menu.NutritionJson) ?? new NutritionInfo();
            }
            catch { }

            return new MealMenuDto
            {
                Id = menu.Id,
                CafeteriaId = menu.CafeteriaId,
                CafeteriaName = menu.Cafeteria?.Name ?? "",
                Date = menu.Date,
                MealType = menu.MealType,
                Items = items,
                Nutrition = nutrition,
                IsPublished = menu.IsPublished,
                HasVegetarianOption = menu.HasVegetarianOption,
                Price = menu.Price
            };
        }

        private MealReservationDto MapToReservationDto(MealReservation reservation)
        {
            var items = new List<string>();
            if (reservation.Menu != null)
            {
                try
                {
                    items = JsonSerializer.Deserialize<List<string>>(reservation.Menu.ItemsJson) ?? new List<string>();
                }
                catch { }
            }

            return new MealReservationDto
            {
                Id = reservation.Id,
                MenuId = reservation.MenuId,
                CafeteriaName = reservation.Cafeteria?.Name ?? "",
                Date = reservation.Date,
                MealType = reservation.MealType,
                MenuItems = items,
                Amount = reservation.Amount,
                QrCode = reservation.QrCode,
                Status = reservation.Status,
                UsedAt = reservation.UsedAt,
                IsScholarship = reservation.IsScholarship,
                CreatedAt = reservation.CreatedAt
            };
        }
    }
}
