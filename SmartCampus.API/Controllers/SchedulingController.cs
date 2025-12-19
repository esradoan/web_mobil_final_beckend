using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;
using System.Text;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/scheduling")]
    [Authorize]
    public class SchedulingController : ControllerBase
    {
        private readonly ISchedulingService _schedulingService;

        public SchedulingController(ISchedulingService schedulingService)
        {
            _schedulingService = schedulingService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Otomatik ders programı oluştur (Admin)
        /// CSP algoritması ile çakışmasız program üretir
        /// </summary>
        [HttpPost("generate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateSchedule([FromBody] GenerateScheduleDto dto)
        {
            var result = await _schedulingService.GenerateScheduleAsync(dto);
            
            if (!result.Success)
                return BadRequest(new { message = result.Message, error = "GenerationFailed" });

            return Ok(result);
        }

        /// <summary>
        /// Dönem programını görüntüle
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSchedule([FromQuery] string semester = "fall", [FromQuery] int? year = null)
        {
            var scheduleYear = year ?? DateTime.Now.Year;
            var result = await _schedulingService.GetScheduleAsync(semester, scheduleYear);
            return Ok(new { data = result });
        }

        /// <summary>
        /// Tek bir schedule kaydını görüntüle
        /// </summary>
        [HttpGet("{scheduleId}")]
        public async Task<IActionResult> GetScheduleById(int scheduleId)
        {
            var result = await _schedulingService.GetScheduleByIdAsync(scheduleId);
            if (result == null)
                return NotFound(new { message = "Schedule not found", error = "NotFound" });
            return Ok(result);
        }

        /// <summary>
        /// Benim programım (öğrenci veya öğretim üyesi)
        /// </summary>
        [HttpGet("my-schedule")]
        public async Task<IActionResult> GetMySchedule([FromQuery] string semester = "fall", [FromQuery] int? year = null)
        {
            var scheduleYear = year ?? DateTime.Now.Year;
            var result = await _schedulingService.GetMyScheduleAsync(GetUserId(), semester, scheduleYear);
            return Ok(new { data = result });
        }

        /// <summary>
        /// iCal formatında dışa aktar (.ics dosyası)
        /// </summary>
        [HttpGet("my-schedule/ical")]
        public async Task<IActionResult> ExportToICal([FromQuery] string semester = "fall", [FromQuery] int? year = null)
        {
            var scheduleYear = year ?? DateTime.Now.Year;
            var icalContent = await _schedulingService.ExportToICalAsync(GetUserId(), semester, scheduleYear);
            
            var bytes = Encoding.UTF8.GetBytes(icalContent);
            return File(bytes, "text/calendar", $"schedule_{semester}_{scheduleYear}.ics");
        }
    }
}
