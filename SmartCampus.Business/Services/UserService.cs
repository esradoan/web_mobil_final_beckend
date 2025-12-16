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
            
            // Role ve DepartmentId'yi Student veya Faculty'den al
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"🔍 User {userId} roles from Identity: [{string.Join(", ", roles)}]");
            
            if (roles.Contains("Student"))
            {
                userDto.Role = UserRole.Student;
                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    userDto.DepartmentId = student.DepartmentId;
                }
                Console.WriteLine($"✅ User {userId} identified as Student");
            }
            else if (roles.Contains("Faculty"))
            {
                userDto.Role = UserRole.Faculty;
                var faculty = await _context.Faculties
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.UserId == userId);
                if (faculty != null)
                {
                    userDto.DepartmentId = faculty.DepartmentId;
                }
                Console.WriteLine($"✅ User {userId} identified as Faculty");
            }
            else if (roles.Contains("Admin"))
            {
                userDto.Role = UserRole.Admin;
                Console.WriteLine($"✅ User {userId} identified as Admin");
            }
            else
            {
                // Eğer hiçbir role bulunamazsa, Student/Faculty tablosundan kontrol et
                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    userDto.Role = UserRole.Student;
                    userDto.DepartmentId = student.DepartmentId;
                    Console.WriteLine($"⚠️ User {userId} has no Identity role but has Student entry - setting as Student");
                }
                else
                {
                    var faculty = await _context.Faculties
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.UserId == userId);
                    if (faculty != null)
                    {
                        userDto.Role = UserRole.Faculty;
                        userDto.DepartmentId = faculty.DepartmentId;
                        Console.WriteLine($"⚠️ User {userId} has no Identity role but has Faculty entry - setting as Faculty");
                    }
                    else
                    {
                        // Hiçbir role bulunamadı - bu bir hata!
                        Console.WriteLine($"❌ ERROR: User {userId} has no role assigned and no Student/Faculty entry!");
                        throw new Exception($"User {userId} has no role assigned. Please contact administrator.");
                    }
                }
            }
            
            Console.WriteLine($"📤 Returning UserDto with Role: {userDto.Role}");
            return userDto;
        }

        public async Task UpdateProfileAsync(int userId, UpdateUserDto updateDto)
        {
             var user = await _userManager.FindByIdAsync(userId.ToString());
             if (user == null) throw new Exception("User not found");

             // Check if user is Admin - Admin can only update email
             var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
             
             if (isAdmin)
             {
                 // Admin can only update email, not firstName, lastName, phoneNumber
                 if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
                 {
                     // Check if email is already taken
                     var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
                     if (existingUser != null && existingUser.Id != user.Id)
                     {
                         throw new Exception("Email is already taken by another user.");
                     }
                     
                     // Directly update email for admin (bypass email confirmation)
                     user.Email = updateDto.Email;
                     user.NormalizedEmail = updateDto.Email.ToUpperInvariant();
                     user.UserName = updateDto.Email; // Username is typically same as email
                     user.NormalizedUserName = updateDto.Email.ToUpperInvariant();
                 }
                 
                 user.UpdatedAt = DateTime.UtcNow;
                 var result = await _userManager.UpdateAsync(user);
                 if (!result.Succeeded)
                 {
                     throw new Exception("Update failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                 }
                 return;
             }

             // For non-admin users, update all fields
             user.FirstName = updateDto.FirstName;
             user.LastName = updateDto.LastName;
             user.PhoneNumber = updateDto.PhoneNumber;
             user.UpdatedAt = DateTime.UtcNow;

             var result2 = await _userManager.UpdateAsync(user);
             if (!result2.Succeeded)
             {
                 throw new Exception("Update failed: " + string.Join(", ", result2.Errors.Select(e => e.Description)));
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
            
            var userDtos = new List<UserDto>();
            
            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserDto>(user);
                
                // EmailConfirmed'i set et
                userDto.IsEmailVerified = await _userManager.IsEmailConfirmedAsync(user);
                
                // Role bilgisini Identity'den al
                var roles = await _userManager.GetRolesAsync(user);
                Console.WriteLine($"🔍 GetAllUsersAsync - User {user.Id} ({user.Email}) roles: [{string.Join(", ", roles)}]");
                
                if (roles.Contains("Student"))
                {
                    userDto.Role = UserRole.Student;
                    var student = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (student != null)
                    {
                        userDto.DepartmentId = student.DepartmentId;
                    }
                }
                else if (roles.Contains("Faculty"))
                {
                    userDto.Role = UserRole.Faculty;
                    var faculty = await _context.Faculties
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.UserId == user.Id);
                    if (faculty != null)
                    {
                        userDto.DepartmentId = faculty.DepartmentId;
                    }
                }
                else if (roles.Contains("Admin"))
                {
                    userDto.Role = UserRole.Admin;
                }
                else
                {
                    // Eğer Identity'de role yoksa, Student/Faculty tablosundan kontrol et
                    var student = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (student != null)
                    {
                        userDto.Role = UserRole.Student;
                        userDto.DepartmentId = student.DepartmentId;
                    }
                    else
                    {
                        var faculty = await _context.Faculties
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.UserId == user.Id);
                        if (faculty != null)
                        {
                            userDto.Role = UserRole.Faculty;
                            userDto.DepartmentId = faculty.DepartmentId;
                        }
                        else
                        {
                            // Varsayılan olarak Admin (eğer hiçbir role bulunamazsa)
                            userDto.Role = UserRole.Admin;
                        }
                    }
                }
                
                Console.WriteLine($"📤 GetAllUsersAsync - UserDto for {user.Email}: Role = {userDto.Role}");
                userDtos.Add(userDto);
            }
            
            return userDtos;
        }
    }
}
