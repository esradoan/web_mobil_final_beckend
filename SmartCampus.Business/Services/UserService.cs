using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using System.Linq; // Added for FirstOrDefault
using Microsoft.EntityFrameworkCore; // For ToListAsync if needed

namespace SmartCampus.Business.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public UserService(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }

        public async Task UpdateProfileAsync(int userId, UpdateUserDto updateDto)
        {
             var user = await _userManager.FindByIdAsync(userId.ToString());
             if (user == null) throw new Exception("User not found");

             // Update fields
             user.FirstName = updateDto.FirstName;
             user.LastName = updateDto.LastName;
             user.PhoneNumber = updateDto.PhoneNumber;
             user.UpdatedAt = DateTime.UtcNow;

             var result = await _userManager.UpdateAsync(user);
             if (!result.Succeeded)
             {
                 throw new Exception("Update failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
             }
        }

        public async Task UpdateProfilePictureAsync(int userId, string pictureUrl)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new Exception("User not found");

            user.ProfilePictureUrl = pictureUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                 throw new Exception("Update failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(int page, int pageSize)
        {
            var users = await _userManager.Users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }
    }
}
