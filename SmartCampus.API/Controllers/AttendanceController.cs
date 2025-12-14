using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.API.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/attendance")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IQrCodeService? _qrCodeService;
        private readonly IAttendanceHubService? _hubService;

        public AttendanceController(
            IAttendanceService attendanceService,
            IQrCodeService? qrCodeService = null,
            IAttendanceHubService? hubService = null)
        {
            _attendanceService = attendanceService;
            _qrCodeService = qrCodeService;
            _hubService = hubService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        // ==================== SESSION MANAGEMENT (Faculty) ====================

        /// <summary>
        /// Yoklama oturumu açma (Faculty)
        /// </summary>
        [HttpPost("sessions")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> CreateSession([FromBody] CreateAttendanceSessionDto dto)
        {
            try
            {
                var result = await _attendanceService.CreateSessionAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetSession), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "SessionCreationFailed" });
            }
        }

        /// <summary>
        /// Oturum detayları
        /// </summary>
        [HttpGet("sessions/{id}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var session = await _attendanceService.GetSessionByIdAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found", error = "NotFound" });
            return Ok(session);
        }

        /// <summary>
        /// Oturumu kapatma (Faculty)
        /// </summary>
        [HttpPut("sessions/{id}/close")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> CloseSession(int id)
        {
            var result = await _attendanceService.CloseSessionAsync(id, GetUserId());
            if (!result)
                return NotFound(new { message = "Session not found or not authorized", error = "NotFound" });
            return Ok(new { message = "Session closed successfully" });
        }

        /// <summary>
        /// Benim oturumlarım (Faculty)
        /// </summary>
        [HttpGet("sessions/my-sessions")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GetMySessions()
        {
            var result = await _attendanceService.GetMySessionsAsync(GetUserId());
            return Ok(new { data = result });
        }

        /// <summary>
        /// Yoklama raporu (Faculty)
        /// </summary>
        [HttpGet("report/{sectionId}")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> GetAttendanceReport(int sectionId)
        {
            try
            {
                var result = await _attendanceService.GetAttendanceReportAsync(sectionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message, error = "NotFound" });
            }
        }

        // ==================== CHECK-IN (Student) ====================

        /// <summary>
        /// Yoklama verme (Student)
        /// GPS kontrolü ve spoofing detection yapılır
        /// </summary>
        [HttpPost("sessions/{id}/checkin")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInRequestDto dto)
        {
            try
            {
                var result = await _attendanceService.CheckInAsync(id, GetUserId(), dto, GetClientIp());
                
                // Broadcast to SignalR if not flagged for distance
                if (_hubService != null && !(result.IsFlagged && result.FlagReason?.Contains("Distance exceeded") == true))
                {
                    var session = await _attendanceService.GetSessionByIdAsync(id);
                    if (session != null)
                    {
                        var user = User;
                        _ = _hubService.NotifyStudentCheckedInAsync(id, new StudentCheckInNotification
                        {
                            StudentId = GetUserId(),
                            StudentName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
                            CheckInTime = DateTime.UtcNow,
                            Distance = result.Distance,
                            IsFlagged = result.IsFlagged,
                            FlagReason = result.FlagReason
                        });
                        _ = _hubService.NotifyAttendanceCountAsync(id, session.AttendedCount, session.TotalStudents);
                    }
                }
                
                if (result.IsFlagged && result.FlagReason?.Contains("Distance exceeded") == true)
                {
                    return BadRequest(new { 
                        message = result.Message, 
                        error = "DistanceExceeded",
                        distance = result.Distance
                    });
                }
                
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, error = "CheckInFailed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "CheckInFailed" });
            }
        }

        /// <summary>
        /// Yoklama durumum (Student)
        /// </summary>
        [HttpGet("my-attendance")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyAttendance()
        {
            var result = await _attendanceService.GetMyAttendanceAsync(GetUserId());
            return Ok(new { data = result });
        }

        // ==================== QR CODE BONUS ====================

        /// <summary>
        /// QR kod görselini al (Faculty) - Base64 PNG
        /// </summary>
        [HttpGet("sessions/{id}/qr")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GetQrCode(int id)
        {
            if (_qrCodeService == null)
                return BadRequest(new { message = "QR code service not available", error = "NotConfigured" });

            try
            {
                var result = await _qrCodeService.GenerateQrCodeImageAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "QrCodeFailed" });
            }
        }

        /// <summary>
        /// QR kodu yenile (Faculty) - 5 saniyede bir çağrılmalı
        /// </summary>
        [HttpPost("sessions/{id}/qr/refresh")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> RefreshQrCode(int id)
        {
            if (_qrCodeService == null)
                return BadRequest(new { message = "QR code service not available", error = "NotConfigured" });

            try
            {
                var newCode = await _qrCodeService.RefreshQrCodeAsync(id, GetUserId());
                var qrImage = await _qrCodeService.GenerateQrCodeImageAsync(id);
                return Ok(qrImage);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "QrRefreshFailed" });
            }
        }

        /// <summary>
        /// QR kod ile yoklama verme (Student)
        /// GPS + QR kod doğrulaması yapılır
        /// </summary>
        [HttpPost("sessions/{id}/checkin-qr")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckInWithQr(int id, [FromBody] QrCheckInRequestDto dto)
        {
            if (_qrCodeService == null)
                return BadRequest(new { message = "QR code service not available", error = "NotConfigured" });

            try
            {
                var result = await _qrCodeService.CheckInWithQrAsync(id, GetUserId(), dto, GetClientIp());
                
                if (!result.Success)
                {
                    return BadRequest(new { 
                        message = result.Message, 
                        error = "QrCheckInFailed",
                        isFlagged = result.IsFlagged,
                        flagReason = result.FlagReason
                    });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "QrCheckInFailed" });
            }
        }

        // ==================== EXCUSE REQUESTS ====================

        /// <summary>
        /// Mazeret bildirme (Student)
        /// </summary>
        [HttpPost("excuse-requests")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateExcuseRequest([FromForm] CreateExcuseRequestDto dto, IFormFile? document)
        {
            try
            {
                string? documentUrl = null;
                
                // Handle file upload
                if (document != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "excuses");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(document.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream);
                    }
                    
                    documentUrl = $"/uploads/excuses/{fileName}";
                }
                
                var result = await _attendanceService.CreateExcuseRequestAsync(GetUserId(), dto, documentUrl);
                return CreatedAtAction(nameof(GetExcuseRequests), result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, error = "ExcuseRequestFailed" });
            }
        }

        /// <summary>
        /// Mazeret listesi (Faculty)
        /// </summary>
        [HttpGet("excuse-requests")]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> GetExcuseRequests()
        {
            var result = await _attendanceService.GetExcuseRequestsAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>
        /// Mazeret onaylama (Faculty)
        /// </summary>
        [HttpPut("excuse-requests/{id}/approve")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApproveExcuse(int id, [FromBody] ReviewExcuseDto dto)
        {
            var result = await _attendanceService.ApproveExcuseAsync(id, GetUserId(), dto.Notes);
            if (!result)
                return NotFound(new { message = "Excuse request not found", error = "NotFound" });
            return Ok(new { message = "Excuse approved successfully" });
        }

        /// <summary>
        /// Mazeret reddetme (Faculty)
        /// </summary>
        [HttpPut("excuse-requests/{id}/reject")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> RejectExcuse(int id, [FromBody] ReviewExcuseDto dto)
        {
            var result = await _attendanceService.RejectExcuseAsync(id, GetUserId(), dto.Notes);
            if (!result)
                return NotFound(new { message = "Excuse request not found", error = "NotFound" });
            return Ok(new { message = "Excuse rejected" });
        }
    }
}
