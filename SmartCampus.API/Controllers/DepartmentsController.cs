using Microsoft.AspNetCore.Mvc;
using SmartCampus.DataAccess;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/departments")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly IGenericRepository<Department> _departmentRepository;
        private readonly CampusDbContext _context;

        public DepartmentsController(IGenericRepository<Department> departmentRepository, CampusDbContext context)
        {
            _departmentRepository = departmentRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var departments = await _context.Departments
                    .Where(d => !d.IsDeleted)
                    .OrderBy(d => d.Name)
                    .Select(d => new
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Code = d.Code,
                        FacultyName = d.FacultyName
                    })
                    .ToListAsync();

                return Ok(departments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Departments listesi alınamadı", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Where(d => d.Id == id && !d.IsDeleted)
                    .Select(d => new
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Code = d.Code,
                        FacultyName = d.FacultyName
                    })
                    .FirstOrDefaultAsync();

                if (department == null)
                {
                    return NotFound(new { message = "Department bulunamadı" });
                }

                return Ok(department);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Department alınamadı", error = ex.Message });
            }
        }
    }
}

