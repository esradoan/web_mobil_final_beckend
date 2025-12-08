using System.Threading.Tasks;
using SmartCampus.Business.DTOs;

namespace SmartCampus.Business.Services
{
    public interface IAuthService
    {
        Task<TokenDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<TokenDto> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(int userId);
        Task VerifyEmailAsync(string userId, string token);
    }
}
