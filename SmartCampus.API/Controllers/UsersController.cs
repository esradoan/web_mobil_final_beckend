using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public UsersController(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDto userDto)
        {
            // Placeholder for Part 1 requirement
            await Task.CompletedTask;
            return Ok(new { message = "Profile updated successfully (Placeholder)" });
        }

        [HttpPost("me/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture()
        {
             // Placeholder for Part 1 requirement
             // Need Multer/File handling here
             await Task.CompletedTask;
             return Ok(new { message = "Profile picture uploaded successfully (Placeholder)", url = "https://example.com/pic.jpg" });
        }

        [HttpGet("")]
        [Authorize(Roles = "Admin")] // Example role check
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
             // Placeholder for Part 1 requirement
             await Task.CompletedTask;
             return Ok(new { message = "User list (Placeholder)", page, limit });
        }
    }
}
