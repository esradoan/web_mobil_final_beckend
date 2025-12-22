using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;
using System.Linq;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/meals")]
    [Authorize]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _mealService;

        public MealsController(IMealService mealService)
        {
            _mealService = mealService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // ==================== CAFETERIAS ====================

        /// <summary>
        /// Aktif yemekhaneleri listele
        /// </summary>
        [HttpGet("cafeterias")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCafeterias()
        {
            var result = await _mealService.GetCafeteriasAsync();
            return Ok(new { data = result });
        }

        // ==================== MENUS ====================

        /// <summary>
        /// Menü listesi (tarih ve yemekhane filtresi)
        /// </summary>
        [HttpGet("menus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenus([FromQuery] DateTime? date, [FromQuery] int? cafeteriaId)
        {
            var result = await _mealService.GetMenusAsync(date, cafeteriaId);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Menü detayı
        /// </summary>
        [HttpGet("menus/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenu(int id)
        {
            var result = await _mealService.GetMenuByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Menu not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Menü oluştur (Admin/Staff)
        /// </summary>
        [HttpPost("menus")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMenu([FromBody] CreateMealMenuDto dto)
        {
            try
            {
                var result = await _mealService.CreateMenuAsync(dto);
                return CreatedAtAction(nameof(GetMenu), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CreateFailed" });
            }
        }

        /// <summary>
        /// Menü güncelle (Admin/Staff)
        /// </summary>
        [HttpPut("menus/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMenu(int id, [FromBody] UpdateMealMenuDto dto)
        {
            var result = await _mealService.UpdateMenuAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "Menu not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Menü sil (Admin/Staff)
        /// </summary>
        [HttpDelete("menus/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var result = await _mealService.DeleteMenuAsync(id);
            if (!result)
                return NotFound(new { message = "Menu not found", error = "NotFound" });
            return Ok(new { message = "Menu deleted successfully" });
        }

        // ==================== RESERVATIONS ====================

        /// <summary>
        /// Yemek rezervasyonu yap
        /// Burslu: Günlük max 2 öğün
        /// Ücretli: Cüzdan bakiye kontrolü
        /// </summary>
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] CreateMealReservationDto dto)
        {
            try
            {
                var result = await _mealService.CreateReservationAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetMyReservations), result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ReservationFailed" });
            }
        }

        /// <summary>
        /// Rezervasyon iptali
        /// En az 2 saat önce olmalı
        /// Ücretli ise iade yapılır
        /// </summary>
        [HttpDelete("reservations/{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var result = await _mealService.CancelReservationAsync(GetUserId(), id);
                if (!result)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Reservation cancelled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CancelFailed" });
            }
        }

        /// <summary>
        /// Rezervasyonlarım listesi
        /// </summary>
        [HttpGet("reservations/my-reservations")]
        public async Task<IActionResult> GetMyReservations()
        {
            var result = await _mealService.GetMyReservationsAsync(GetUserId());
            return Ok(new { data = result });
        }

        /// <summary>
        /// QR kod ile rezervasyon doğrulama (Cafeteria Staff) - Status değiştirmez
        /// </summary>
        [HttpPost("reservations/validate")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ValidateReservation([FromBody] UseReservationDto dto)
        {
            // Debug: Check user roles
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).ToList();
            Console.WriteLine($"🔍 ValidateReservation - User ID: {GetUserId()}, Roles: [{string.Join(", ", userRoles)}]");
            Console.WriteLine($"🔍 IsInRole Admin: {User.IsInRole("Admin")}, IsInRole Faculty: {User.IsInRole("Faculty")}");
            
            try
            {
                var result = await _mealService.ValidateReservationByQrCodeAsync(dto.QrCode);
                if (result == null)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Reservation validated", reservation = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ValidationFailed" });
            }
        }

        /// <summary>
        /// QR kod ile yemek kullanımı (Cafeteria Staff)
        /// </summary>
        [HttpPost("reservations/use")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> UseReservation([FromBody] UseReservationDto dto)
        {
            // Debug: Check user roles
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).ToList();
            Console.WriteLine($"🔍 UseReservation - User ID: {GetUserId()}, Roles: [{string.Join(", ", userRoles)}]");
            Console.WriteLine($"🔍 IsInRole Admin: {User.IsInRole("Admin")}, IsInRole Faculty: {User.IsInRole("Faculty")}");
            
            try
            {
                var result = await _mealService.UseReservationAsync(dto.QrCode);
                if (result == null)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Meal confirmed", reservation = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "UseFailed" });
            }
        }
    }

    public class UseReservationDto
    {
        public string QrCode { get; set; } = string.Empty;
    }
}
