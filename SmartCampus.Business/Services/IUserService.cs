using System.Collections.Generic;
using System.Threading.Tasks;
using SmartCampus.Business.DTOs;

namespace SmartCampus.Business.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UpdateUserDto updateDto);
        Task UpdateProfilePictureAsync(int userId, string pictureUrl);
        // Admin method
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
    }
}
