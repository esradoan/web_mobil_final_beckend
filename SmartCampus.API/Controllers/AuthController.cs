using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return proper error response
                return BadRequest(new { 
                    message = ex.Message,
                    error = "Registration failed"
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (loginDto == null)
                {
                    return BadRequest(new { message = "Login data is required" });
                }

                var result = await _authService.LoginAsync(loginDto);
                
                // Ensure tokens are present
                if (result == null)
                {
                    return BadRequest(new { message = "Login failed: No result returned" });
                }
                
                if (string.IsNullOrEmpty(result.AccessToken) || string.IsNullOrEmpty(result.RefreshToken))
                {
                    return BadRequest(new { 
                        message = "Token generation failed",
                        accessTokenPresent = !string.IsNullOrEmpty(result.AccessToken),
                        refreshTokenPresent = !string.IsNullOrEmpty(result.RefreshToken)
                    });
                }
                
                // Return TokenDto (will be serialized as camelCase: accessToken, refreshToken, expiration)
                return Ok(new
                {
                    AccessToken = result.AccessToken,
                    RefreshToken = result.RefreshToken,
                    Expiration = result.Expiration
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    message = ex.Message,
                    error = "Login failed",
                    detailed = ex.ToString() // For debugging - remove in production
                });
            }
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
            try
            {
                // Debug: Request'i logla
                Console.WriteLine($"\n📥 VERIFY EMAIL REQUEST:");
                Console.WriteLine($"   UserId: {verifyDto.UserId}");
                Console.WriteLine($"   Token length: {verifyDto.Token?.Length ?? 0}");
                Console.WriteLine($"   Token (first 50): {verifyDto.Token?.Substring(0, Math.Min(50, verifyDto.Token?.Length ?? 0))}...");
                
                if (string.IsNullOrEmpty(verifyDto.UserId) || string.IsNullOrEmpty(verifyDto.Token))
                {
                    Console.WriteLine($"❌ Missing UserId or Token");
                    return BadRequest(new { message = "UserId and Token are required" });
                }
                
                await _authService.VerifyEmailAsync(verifyDto.UserId, verifyDto.Token);
                
                // Email doğrulandıktan sonra güncel user bilgilerini döndür
                var userService = HttpContext.RequestServices.GetRequiredService<IUserService>();
                var userDto = await userService.GetProfileAsync(int.Parse(verifyDto.UserId));
                
                Console.WriteLine($"✅ Email verification successful for user {verifyDto.UserId}");
                
                return Ok(new { 
                    message = "Email verified successfully",
                    user = userDto 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Verify Email Error: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("resend-verification-email")]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailDto dto)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(dto.Email);
                return Ok(new { message = "Verification email sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto.Email);
                return Ok(new { message = "If user exists, reset link sent" });
            }
            catch (Exception ex)
            {
                // Log the error but don't reveal if user exists (security)
                return BadRequest(new { message = "Email gönderilemedi. Lütfen SMTP ayarlarını kontrol edin veya daha sonra tekrar deneyin." });
            }
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

    public class ResendVerificationEmailDto
    {
        public string Email { get; set; } = string.Empty;
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
