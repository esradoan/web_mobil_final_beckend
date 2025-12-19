using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // ==================== EVENTS ====================

        /// <summary>
        /// Etkinlik listesi (kategori ve tarih filtresi)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents([FromQuery] string? category, [FromQuery] DateTime? date)
        {
            var result = await _eventService.GetEventsAsync(category, date);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Etkinlik detayı
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvent(int id)
        {
            var result = await _eventService.GetEventByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Event not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Etkinlik oluştur (Admin/Event Manager)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var result = await _eventService.CreateEventAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetEvent), new { id = result.Id }, result);
        }

        /// <summary>
        /// Etkinlik güncelle
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var result = await _eventService.UpdateEventAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "Event not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Etkinlik iptal et
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _eventService.DeleteEventAsync(id);
            if (!result)
                return NotFound(new { message = "Event not found", error = "NotFound" });
            return Ok(new { message = "Event cancelled successfully" });
        }

        // ==================== REGISTRATIONS ====================

        /// <summary>
        /// Etkinliğe kayıt ol
        /// </summary>
        [HttpPost("{id}/register")]
        public async Task<IActionResult> Register(int id)
        {
            try
            {
                var result = await _eventService.RegisterAsync(GetUserId(), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "RegistrationFailed" });
            }
        }

        /// <summary>
        /// Kayıt iptal et
        /// </summary>
        [HttpDelete("registrations/{registrationId}")]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            try
            {
                var result = await _eventService.CancelRegistrationAsync(GetUserId(), registrationId);
                if (!result)
                    return NotFound(new { message = "Registration not found", error = "NotFound" });
                return Ok(new { message = "Registration cancelled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CancelFailed" });
            }
        }

        /// <summary>
        /// Etkinliğe kayıtlı kullanıcılar (Event Manager)
        /// </summary>
        [HttpGet("{id}/registrations")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> GetRegistrations(int id)
        {
            var result = await _eventService.GetEventRegistrationsAsync(id);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Benim etkinliklerim
        /// </summary>
        [HttpGet("my-events")]
        public async Task<IActionResult> GetMyEvents()
        {
            var result = await _eventService.GetMyRegistrationsAsync(GetUserId());
            return Ok(new { data = result });
        }

        /// <summary>
        /// QR ile check-in (Event Manager)
        /// </summary>
        [HttpPost("{eventId}/checkin")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> CheckIn(int eventId, [FromBody] CheckInDto dto)
        {
            try
            {
                var result = await _eventService.CheckInAsync(eventId, dto.QrCode);
                if (result == null)
                    return NotFound(new { message = "Registration not found", error = "NotFound" });
                return Ok(new { message = "Check-in successful", registration = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CheckInFailed" });
            }
        }
    }

    public class CheckInDto
    {
        public string QrCode { get; set; } = string.Empty;
    }
}
