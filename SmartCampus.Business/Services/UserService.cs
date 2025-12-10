using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using System.Linq; // Added for FirstOrDefault
using Microsoft.EntityFrameworkCore; // For ToListAsync if needed
using SmartCampus.DataAccess;

namespace SmartCampus.Business.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly CampusDbContext _context;

        public UserService(UserManager<User> userManager, IMapper mapper, CampusDbContext context)
        {
            _userManager = userManager;
            _mapper = mapper;
            _context = context;
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            // Database'den fresh user çek (cache'lenmiş user yerine)
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return null;
            
            // EmailConfirmed field'ını kontrol et ve logla
            var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            Console.WriteLine($"🔍 User {userId} EmailConfirmed status: {emailConfirmed}");
            
            // User objesini mapper ile UserDto'ya çevir
            var userDto = _mapper.Map<UserDto>(user);
            
            // EmailConfirmed'i manuel olarak set et (mapping'den sonra)
            userDto.IsEmailVerified = emailConfirmed;
            
            return userDto;
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
