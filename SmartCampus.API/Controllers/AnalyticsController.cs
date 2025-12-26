using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/analytics")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAttendanceAnalyticsService _analyticsService;

        public AnalyticsController(IAttendanceAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Ders section yoklama trendleri (Faculty)
        /// </summary>
        [HttpGet("sections/{sectionId}/trends")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> GetAttendanceTrends(int sectionId)
        {
            try
            {
                var result = await _analyticsService.GetAttendanceTrendsAsync(sectionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Öğrenci risk analizi (Student - kendi, Faculty - herhangi)
        /// </summary>
        [HttpGet("students/{studentId}/risk")]
        public async Task<IActionResult> GetStudentRiskAnalysis(int studentId)
        {
            // Students can only view their own analysis
            if (User.IsInRole("Student") && studentId != GetUserId())
            {
                return Forbid();
            }

            try
            {
                var result = await _analyticsService.GetStudentRiskAnalysisAsync(studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Benim risk analizim (Student)
        /// </summary>
        [HttpGet("my-risk")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyRiskAnalysis()
        {
            try
            {
                var result = await _analyticsService.GetStudentRiskAnalysisAsync(GetUserId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Section genel analizi (Faculty)
        /// </summary>
        [HttpGet("sections/{sectionId}")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> GetSectionAnalytics(int sectionId)
        {
            try
            {
                var result = await _analyticsService.GetSectionAnalyticsAsync(sectionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        /// <summary>
        /// Kampüs genel istatistikleri (Admin)
        /// </summary>
        [HttpGet("campus")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCampusAnalytics()
        {
            var result = await _analyticsService.GetCampusAnalyticsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Section raporunu PDF olarak indir
        /// </summary>
        [HttpGet("sections/{sectionId}/export/pdf")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> ExportSectionPdf(int sectionId)
        {
            try
            {
                var pdfBytes = await _analyticsService.ExportSectionReportAsync(sectionId);
                return File(pdfBytes, "application/pdf", $"AttendanceReport_Section{sectionId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Section raporunu Excel (CSV) olarak indir
        /// </summary>
        [HttpGet("sections/{sectionId}/export/excel")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> ExportSectionExcel(int sectionId)
        {
             try
            {
                var csvBytes = await _analyticsService.ExportSectionReportToExcelAsync(sectionId);
                return File(csvBytes, "text/csv", $"AttendanceReport_Section{sectionId}_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
