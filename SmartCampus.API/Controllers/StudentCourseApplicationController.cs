using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.Entities;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/student-course-applications")]
    [Authorize]
    public class StudentCourseApplicationController : ControllerBase
    {
        private readonly IStudentCourseApplicationService _applicationService;

        public StudentCourseApplicationController(IStudentCourseApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        /// <summary>
        /// Öğrenci ders başvurusu yapar
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateStudentCourseApplicationDto dto)
        {
            try
            {
                var studentId = GetUserId();
                var application = await _applicationService.CreateApplicationAsync(dto.CourseId, dto.SectionId, studentId);
                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ApplicationFailed" });
            }
        }

        /// <summary>
        /// Başvuruları listele (Admin: tümü, Student: kendi başvuruları)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApplications(
            [FromQuery] ApplicationStatus? status = null,
            [FromQuery] int? studentId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // Student sadece kendi başvurularını görebilir
                if (userRole == "Student")
                {
                    studentId = userId;
                }
                // Admin tüm başvuruları görebilir, studentId parametresi opsiyonel

                var result = await _applicationService.GetApplicationsAsync(studentId, status, page, pageSize);
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

                // Student sadece kendi başvurularını görebilir
                if (userRole == "Student" && application.StudentId != userId)
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
        public async Task<IActionResult> RejectApplication(int id, [FromBody] RejectStudentApplicationDto? dto = null)
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
        /// Öğrencinin bu section'a başvuru yapıp yapamayacağını kontrol et
        /// </summary>
        [HttpGet("can-apply")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CanApply([FromQuery] int sectionId)
        {
            try
            {
                var studentId = GetUserId();
                var canApply = await _applicationService.CanStudentApplyAsync(studentId, sectionId);
                return Ok(new { canApply });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CheckFailed" });
            }
        }

        /// <summary>
        /// Öğrenci için mevcut dersleri getir (hocası olanlar önce)
        /// </summary>
        [HttpGet("available-courses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAvailableCourses()
        {
            try
            {
                var studentId = GetUserId();
                var courses = await _applicationService.GetAvailableCoursesForStudentAsync(studentId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "GetCoursesFailed" });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }

    public class RejectStudentApplicationDto
    {
        public string? Reason { get; set; }
    }
}

