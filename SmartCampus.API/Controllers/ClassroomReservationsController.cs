using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/reservations")]
    [Authorize]
    public class ClassroomReservationsController : ControllerBase
    {
        private readonly IClassroomReservationService _reservationService;

        public ClassroomReservationsController(IClassroomReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // ==================== RESERVATIONS ====================

        /// <summary>
        /// Derslik rezervasyonu oluştur
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateClassroomReservationDto dto)
        {
            try
            {
                var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetReservation), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ReservationFailed" });
            }
        }

        /// <summary>
        /// Tüm rezervasyonları listele (filtreli)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReservations(
            [FromQuery] int? classroomId, 
            [FromQuery] DateTime? date, 
            [FromQuery] string? status)
        {
            var result = await _reservationService.GetReservationsAsync(classroomId, date, status);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Rezervasyon detayı
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservation(int id)
        {
            var result = await _reservationService.GetReservationByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Reservation not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Benim rezervasyonlarım
        /// </summary>
        [HttpGet("my-reservations")]
        public async Task<IActionResult> GetMyReservations()
        {
            var result = await _reservationService.GetMyReservationsAsync(GetUserId());
            return Ok(new { data = result });
        }

        /// <summary>
        /// Rezervasyon iptal et
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var result = await _reservationService.CancelReservationAsync(GetUserId(), id);
                if (!result)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Reservation cancelled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CancelFailed" });
            }
        }

        // ==================== APPROVAL WORKFLOW ====================

        /// <summary>
        /// Bekleyen rezervasyonları listele (Admin)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> GetPendingReservations()
        {
            var result = await _reservationService.GetPendingReservationsAsync();
            return Ok(new { data = result });
        }

        /// <summary>
        /// Rezervasyonu onayla (Admin)
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ApproveReservation(int id, [FromBody] ApprovalDto? dto)
        {
            try
            {
                var result = await _reservationService.ApproveReservationAsync(GetUserId(), id, dto?.Notes);
                if (result == null)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Reservation approved", reservation = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ApprovalFailed" });
            }
        }

        /// <summary>
        /// Rezervasyonu reddet (Admin)
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> RejectReservation(int id, [FromBody] ApprovalDto? dto)
        {
            try
            {
                var result = await _reservationService.RejectReservationAsync(GetUserId(), id, dto?.Notes);
                if (result == null)
                    return NotFound(new { message = "Reservation not found", error = "NotFound" });
                return Ok(new { message = "Reservation rejected", reservation = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "RejectionFailed" });
            }
        }

        // ==================== AVAILABILITY ====================

        /// <summary>
        /// Derslik müsaitlik durumu
        /// </summary>
        [HttpGet("classrooms/{classroomId}/availability")]
        public async Task<IActionResult> GetClassroomAvailability(int classroomId, [FromQuery] DateTime date)
        {
            var result = await _reservationService.GetClassroomAvailabilityAsync(classroomId, date);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Müsait derslikleri listele
        /// </summary>
        [HttpGet("classrooms/available")]
        public async Task<IActionResult> GetAvailableClassrooms(
            [FromQuery] DateTime date, 
            [FromQuery] TimeSpan startTime, 
            [FromQuery] TimeSpan endTime)
        {
            var result = await _reservationService.GetAvailableClassroomsAsync(date, startTime, endTime);
            return Ok(new { data = result });
        }
    }

    public class ApprovalDto
    {
        public string? Notes { get; set; }
    }
}
