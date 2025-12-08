using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System.Threading.Tasks;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshDto.RefreshToken);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // In a real app we would get userId from User.Claims
            // For now, since logout endpoint might require auth:
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                 await _authService.LogoutAsync(userId);
            }
            return NoContent();
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto verifyDto)
        {
            await _authService.VerifyEmailAsync(verifyDto.UserId, verifyDto.Token);
            return Ok(new { message = "Email verified successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "If user exists, reset link sent" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Passwords do not match");

            await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("password-strength")]
        public IActionResult CheckPasswordStrength([FromBody] string password)
        {
            var result = SmartCampus.Business.Helpers.PasswordStrength.Evaluate(password);
            return Ok(new { Score = result.Score, Feedback = result.Feedback });
        }
    }

    public class VerifyEmailDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class RefreshTokenDto 
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
