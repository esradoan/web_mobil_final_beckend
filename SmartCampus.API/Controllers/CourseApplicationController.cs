using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/course-applications")]
    [Authorize]
    public class CourseApplicationController : ControllerBase
    {
        private readonly ICourseApplicationService _applicationService;

        public CourseApplicationController(ICourseApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        /// <summary>
        /// Öğretmen ders başvurusu yapar
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateCourseApplicationDto dto)
        {
            try
            {
                var instructorId = GetUserId();
                var application = await _applicationService.CreateApplicationAsync(dto.CourseId, instructorId);
                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ApplicationFailed" });
            }
        }

        /// <summary>
        /// Başvuruları listele (Admin: tümü, Faculty: kendi başvuruları)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApplications(
            [FromQuery] ApplicationStatus? status = null,
            [FromQuery] int? instructorId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // Faculty sadece kendi başvurularını görebilir
                if (userRole == "Faculty")
                {
                    instructorId = userId;
                }
                // Admin tüm başvuruları görebilir, instructorId parametresi opsiyonel

                var result = await _applicationService.GetApplicationsAsync(instructorId, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "GetApplicationsFailed" });
            }
        }

        /// <summary>
        /// Başvuru detayları
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetApplication(int id)
        {
            try
            {
                var application = await _applicationService.GetApplicationByIdAsync(id);
                if (application == null)
                    return NotFound(new { message = "Başvuru bulunamadı.", error = "NotFound" });

                var userId = GetUserId();
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // Faculty sadece kendi başvurularını görebilir
                if (userRole == "Faculty" && application.InstructorId != userId)
                {
                    return Forbid();
                }

                return Ok(application);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "GetApplicationFailed" });
            }
        }

        /// <summary>
        /// Başvuruyu onayla (Admin only)
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            try
            {
                var adminUserId = GetUserId();
                var application = await _applicationService.ApproveApplicationAsync(id, adminUserId);
                return Ok(application);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ApproveFailed" });
            }
        }

        /// <summary>
        /// Başvuruyu reddet (Admin only)
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectApplication(int id, [FromBody] RejectApplicationDto? dto = null)
        {
            try
            {
                var adminUserId = GetUserId();
                var reason = dto?.Reason;
                var application = await _applicationService.RejectApplicationAsync(id, adminUserId, reason);
                return Ok(application);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "RejectFailed" });
            }
        }

        /// <summary>
        /// Öğretmenin bu course'a başvuru yapıp yapamayacağını kontrol et
        /// </summary>
        [HttpGet("can-apply")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> CanApply([FromQuery] int courseId)
        {
            try
            {
                var instructorId = GetUserId();
                var canApply = await _applicationService.CanInstructorApplyAsync(instructorId, courseId);
                return Ok(new { canApply });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CheckFailed" });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }

    public class RejectApplicationDto
    {
        public string? Reason { get; set; }
    }
}

