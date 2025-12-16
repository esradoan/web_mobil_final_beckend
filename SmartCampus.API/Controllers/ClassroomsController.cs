using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/classrooms")]
    [ApiController]
    [Authorize] // Authenticated users can access
    public class ClassroomsController : ControllerBase
    {
        private readonly CampusDbContext _context;

        public ClassroomsController(CampusDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm sınıfları listele (Section yönetimi için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllClassrooms()
        {
            try
            {
                var classrooms = await _context.Classrooms
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Building)
                    .ThenBy(c => c.RoomNumber)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Building = c.Building,
                        RoomNumber = c.RoomNumber,
                        Capacity = c.Capacity,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        FeaturesJson = c.FeaturesJson,
                        FullName = $"{c.Building} - {c.RoomNumber}"
                    })
                    .ToListAsync();

                return Ok(new { data = classrooms });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Classroom listesi alınamadı", error = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir sınıfın detaylarını getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassroomById(int id)
        {
            try
            {
                var classroom = await _context.Classrooms
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Building = c.Building,
                        RoomNumber = c.RoomNumber,
                        Capacity = c.Capacity,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        FeaturesJson = c.FeaturesJson,
                        FullName = $"{c.Building} - {c.RoomNumber}"
                    })
                    .FirstOrDefaultAsync();

                if (classroom == null)
                {
                    return NotFound(new { message = "Classroom bulunamadı" });
                }

                return Ok(classroom);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Classroom alınamadı", error = ex.Message });
            }
        }
    }
}

