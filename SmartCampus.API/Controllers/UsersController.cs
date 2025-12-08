using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using SmartCampus.Business.Services;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null) return Unauthorized();

            var userDto = await _userService.GetProfileAsync(int.Parse(userIdStr));
            if (userDto == null) return NotFound("User not found.");
            return Ok(userDto);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto updateDto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
           if (userIdStr == null) return Unauthorized();

            await _userService.UpdateProfileAsync(int.Parse(userIdStr), updateDto);
            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("me/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null) return Unauthorized();

            // Validate file type etc. (Basic check)
            if (!file.ContentType.StartsWith("image/"))
                 return BadRequest("Only image files are allowed.");

            // Save file
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{uniqueFileName}"; // Relative URL
            
            await _userService.UpdateProfilePictureAsync(int.Parse(userIdStr), fileUrl);

            return Ok(new { message = "Profile picture uploaded successfully", url = fileUrl });
        }

        [HttpGet("")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetUsers()
        {
             var users = await _userService.GetAllUsersAsync();
             return Ok(users);
        }
    }
}
