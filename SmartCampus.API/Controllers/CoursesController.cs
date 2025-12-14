using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Ders listesi (pagination, filtering, search)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCourses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? departmentId = null,
            [FromQuery] string? sort = null)
        {
            var result = await _courseService.GetCoursesAsync(page, limit, search, departmentId, sort);
            return Ok(result);
        }

        /// <summary>
        /// Ders detayları (prerequisites dahil)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound(new { message = "Course not found", error = "NotFound" });
            return Ok(course);
        }

        /// <summary>
        /// Yeni ders oluşturma (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            try
            {
                var course = await _courseService.CreateCourseAsync(dto);
                return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CreateFailed" });
            }
        }

        /// <summary>
        /// Ders güncelleme (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            var course = await _courseService.UpdateCourseAsync(id, dto);
            if (course == null)
                return NotFound(new { message = "Course not found", error = "NotFound" });
            return Ok(course);
        }

        /// <summary>
        /// Ders silme - soft delete (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var result = await _courseService.DeleteCourseAsync(id);
            if (!result)
                return NotFound(new { message = "Course not found", error = "NotFound" });
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/v1/sections")]
    public class SectionsController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public SectionsController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Section listesi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSections(
            [FromQuery] string? semester = null,
            [FromQuery] int? year = null,
            [FromQuery] int? instructorId = null,
            [FromQuery] int? courseId = null)
        {
            var sections = await _courseService.GetSectionsAsync(semester, year, instructorId, courseId);
            return Ok(new { data = sections });
        }

        /// <summary>
        /// Section detayları
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSection(int id)
        {
            var section = await _courseService.GetSectionByIdAsync(id);
            if (section == null)
                return NotFound(new { message = "Section not found", error = "NotFound" });
            return Ok(section);
        }

        /// <summary>
        /// Section oluşturma (Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionDto dto)
        {
            try
            {
                var section = await _courseService.CreateSectionAsync(dto);
                return CreatedAtAction(nameof(GetSection), new { id = section.Id }, section);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CreateFailed" });
            }
        }

        /// <summary>
        /// Section güncelleme (Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSection(int id, [FromBody] UpdateSectionDto dto)
        {
            var section = await _courseService.UpdateSectionAsync(id, dto);
            if (section == null)
                return NotFound(new { message = "Section not found", error = "NotFound" });
            return Ok(section);
        }
    }
}
