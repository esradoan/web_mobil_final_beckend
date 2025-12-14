using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/enrollments")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Derse kayıt olma (Student)
        /// Ön koşul, çakışma ve kapasite kontrolü yapılır
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll([FromBody] CreateEnrollmentDto dto)
        {
            try
            {
                var result = await _enrollmentService.EnrollAsync(GetUserId(), dto.SectionId);
                return CreatedAtAction(nameof(GetMyCourses), result);
            }
            catch (InvalidOperationException ex)
            {
                var error = ex.Message switch
                {
                    var m when m.Contains("Prerequisite") => "PrerequisiteCheckFailed",
                    var m when m.Contains("conflict") => "ScheduleConflict",
                    var m when m.Contains("full") => "CapacityExceeded",
                    var m when m.Contains("Already") => "AlreadyEnrolled",
                    _ => "EnrollmentFailed"
                };
                return BadRequest(new { message = ex.Message, error });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "EnrollmentFailed" });
            }
        }

        /// <summary>
        /// Dersi bırakma (Student) - İlk 4 hafta kontrolü
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DropCourse(int id)
        {
            try
            {
                var result = await _enrollmentService.DropCourseAsync(id, GetUserId());
                if (!result)
                    return NotFound(new { message = "Enrollment not found", error = "NotFound" });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, error = "DropNotAllowed" });
            }
        }

        /// <summary>
        /// Kayıtlı derslerim (Student)
        /// </summary>
        [HttpGet("my-courses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyCourses()
        {
            var result = await _enrollmentService.GetMyCoursesAsync(GetUserId());
            return Ok(new { data = result });
        }

        /// <summary>
        /// Dersin öğrenci listesi (Faculty)
        /// </summary>
        [HttpGet("students/{sectionId}")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> GetSectionStudents(int sectionId)
        {
            var result = await _enrollmentService.GetSectionStudentsAsync(sectionId);
            return Ok(result);
        }
    }

    [ApiController]
    [Route("api/v1/grades")]
    [Authorize]
    public class GradesController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ITranscriptPdfService _pdfService;

        public GradesController(IEnrollmentService enrollmentService, ITranscriptPdfService pdfService)
        {
            _enrollmentService = enrollmentService;
            _pdfService = pdfService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Notlarım (Student)
        /// </summary>
        [HttpGet("my-grades")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyGrades()
        {
            var result = await _enrollmentService.GetMyGradesAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>
        /// Transkript JSON (Student)
        /// </summary>
        [HttpGet("transcript")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTranscript()
        {
            try
            {
                var result = await _enrollmentService.GetTranscriptAsync(GetUserId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Transkript PDF (Student)
        /// QuestPDF ile profesyonel PDF oluşturma
        /// </summary>
        [HttpGet("transcript/pdf")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTranscriptPdf()
        {
            try
            {
                var transcript = await _enrollmentService.GetTranscriptAsync(GetUserId());
                var pdfBytes = _pdfService.GenerateTranscript(transcript);
                
                return File(pdfBytes, "application/pdf", $"transcript_{transcript.StudentNumber}.pdf");
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Not girişi (Faculty)
        /// Harf notu ve grade point otomatik hesaplanır
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> EnterGrade([FromBody] GradeInputDto dto)
        {
            try
            {
                var result = await _enrollmentService.EnterGradeAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "GradeEntryFailed" });
            }
        }
    }
}
