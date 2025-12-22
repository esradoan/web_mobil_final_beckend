using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize] // Base authorization - individual methods will specify roles
    public class AdminController : ControllerBase
    {
        private readonly CampusDbContext _context;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ITranscriptPdfService _pdfService;
        private readonly UserManager<User> _userManager;

        public AdminController(
            CampusDbContext context,
            IEnrollmentService enrollmentService,
            ITranscriptPdfService pdfService,
            UserManager<User> userManager)
        {
            _context = context;
            _enrollmentService = enrollmentService;
            _pdfService = pdfService;
            _userManager = userManager;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Tüm öğrencileri listele (Admin) veya bölüm öğrencilerini listele (Faculty)
        /// </summary>
        [HttpGet("students")]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> GetStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null,
            [FromQuery] int? departmentId = null)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .AsQueryable();

            // Faculty ise sadece kendi bölümündeki öğrencileri göster
            if (User.IsInRole("Faculty") && !User.IsInRole("Admin"))
            {
                var facultyUserId = GetUserId();
                if (facultyUserId > 0)
                {
                    var faculty = await _context.Faculties
                        .FirstOrDefaultAsync(f => f.UserId == facultyUserId);
                    if (faculty != null)
                    {
                        query = query.Where(s => s.DepartmentId == faculty.DepartmentId);
                        Console.WriteLine($"✅ Faculty {facultyUserId} filtering students by department {faculty.DepartmentId}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Faculty entry not found for user {facultyUserId}");
                        return Forbid("Faculty bilgisi bulunamadı");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Invalid user ID: {facultyUserId}");
                    return Forbid("Geçersiz kullanıcı ID'si");
                }
            }
            // Admin ise departmentId parametresi ile filtreleme yapabilir
            else if (departmentId.HasValue)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }

            // Aktif/Pasif filtreleme
            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            // Arama (öğrenci numarası, ad, soyad, email)
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s =>
                    s.StudentNumber.ToLower().Contains(searchLower) ||
                    (s.User != null && s.User.FirstName != null && s.User.FirstName.ToLower().Contains(searchLower)) ||
                    (s.User != null && s.User.LastName != null && s.User.LastName.ToLower().Contains(searchLower)) ||
                    (s.User != null && s.User.Email != null && s.User.Email.ToLower().Contains(searchLower)));
            }

            var total = await query.CountAsync();

            var students = await query
                .OrderBy(s => s.User != null ? s.User.LastName : "")
                .ThenBy(s => s.User != null ? s.User.FirstName : "")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    StudentNumber = s.StudentNumber,
                    FirstName = s.User != null ? s.User.FirstName ?? "" : "",
                    LastName = s.User != null ? s.User.LastName ?? "" : "",
                    Email = s.User != null ? s.User.Email ?? "" : "",
                    DepartmentId = s.DepartmentId,
                    DepartmentName = s.Department != null ? s.Department.Name : "",
                    GPA = s.GPA,
                    CGPA = s.CGPA,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(new StudentListResponseDto
            {
                Data = students,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Öğrenci durumunu güncelle (Aktif/Pasif)
        /// </summary>
        [HttpPut("students/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStudentStatus(int id, [FromBody] UpdateStudentStatusDto dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "Öğrenci bulunamadı." });
            }

            student.IsActive = dto.IsActive;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Öğrenci durumu {(dto.IsActive ? "aktif" : "pasif")} olarak güncellendi.", isActive = dto.IsActive });
        }

        /// <summary>
        /// Öğrenci transkriptini görüntüle (Admin)
        /// </summary>
        [HttpGet("students/{id}/transcript")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentTranscript(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new { message = "Öğrenci bulunamadı." });
            }

            var transcript = await _enrollmentService.GetTranscriptAsync(student.UserId);
            return Ok(transcript);
        }

        /// <summary>
        /// Öğrenci transkript PDF'ini indir (Admin)
        /// </summary>
        [HttpGet("students/{id}/transcript/pdf")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentTranscriptPdf(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new { message = "Öğrenci bulunamadı." });
            }

            var transcript = await _enrollmentService.GetTranscriptAsync(student.UserId);
            var pdfBytes = _pdfService.GenerateTranscript(transcript);

            return File(pdfBytes, "application/pdf", $"transcript_{student.StudentNumber}.pdf");
        }

        /// <summary>
        /// Mevcut kullanıcılar için otomatik role ataması yap (Admin only)
        /// Student veya Faculty tablosunda kaydı olan ama Identity'de role'ü olmayan kullanıcılara role atar
        /// </summary>
        [HttpPost("fix-user-roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FixUserRoles()
        {
            try
            {
                var fixedUsers = new List<object>();
                var errors = new List<string>();

                // Student tablosundaki tüm kullanıcıları kontrol et
                var students = await _context.Students
                    .Include(s => s.User)
                    .ToListAsync();

                foreach (var student in students)
                {
                    var user = student.User;
                    if (user == null) continue;

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("Student"))
                    {
                        var result = await _userManager.AddToRoleAsync(user, "Student");
                        if (result.Succeeded)
                        {
                            fixedUsers.Add(new { userId = user.Id, email = user.Email, role = "Student" });
                            Console.WriteLine($"✅ Assigned Student role to user {user.Id} ({user.Email})");
                        }
                        else
                        {
                            errors.Add($"Failed to assign Student role to user {user.Id}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                }

                // Faculty tablosundaki tüm kullanıcıları kontrol et
                var faculties = await _context.Faculties
                    .Include(f => f.User)
                    .ToListAsync();

                foreach (var faculty in faculties)
                {
                    var user = faculty.User;
                    if (user == null) continue;

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("Faculty"))
                    {
                        var result = await _userManager.AddToRoleAsync(user, "Faculty");
                        if (result.Succeeded)
                        {
                            fixedUsers.Add(new { userId = user.Id, email = user.Email, role = "Faculty" });
                            Console.WriteLine($"✅ Assigned Faculty role to user {user.Id} ({user.Email})");
                        }
                        else
                        {
                            errors.Add($"Failed to assign Faculty role to user {user.Id}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                }

                return Ok(new
                {
                    message = $"Role ataması tamamlandı. {fixedUsers.Count} kullanıcıya role atandı.",
                    fixedUsers,
                    errors,
                    totalFixed = fixedUsers.Count,
                    totalErrors = errors.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Role ataması sırasında hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcıyı email ile bul ve durumunu kontrol et (Admin only)
        /// </summary>
        [HttpGet("users/check/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckUserStatus(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı.", email });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
                
                // Check if user has Student or Faculty entry
                var hasStudentEntry = await _context.Students.AnyAsync(s => s.UserId == user.Id);
                var hasFacultyEntry = await _context.Faculties.AnyAsync(f => f.UserId == user.Id);

                return Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    emailConfirmed,
                    isLockedOut,
                    roles = roles.ToList(),
                    hasStudentEntry,
                    hasFacultyEntry,
                    createdAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı kontrolü sırasında hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcıyı email ile sil (Admin only)
        /// </summary>
        [HttpDelete("users/delete/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserByEmail(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı.", email });
                }

                // Delete related entries first
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    _context.Students.Remove(student);
                }

                var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == user.Id);
                if (faculty != null)
                {
                    _context.Faculties.Remove(faculty);
                }

                await _context.SaveChangesAsync();

                // Delete from Identity
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Ok(new { message = $"Kullanıcı {email} başarıyla silindi.", userId = user.Id });
                }
                else
                {
                    return BadRequest(new { message = "Kullanıcı silinirken hata oluştu", errors = result.Errors.Select(e => e.Description) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı silme sırasında hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcıyı güncelle (Admin only)
        /// </summary>
        [HttpPut("users/update/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı.", email });
                }

                // Check if target user is Admin - Admin users have restricted updates
                var targetIsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Update email if provided and different
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Check if email is already taken
                    var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest(new { message = "Bu email adresi zaten kullanılıyor." });
                    }
                    
                    user.Email = dto.Email;
                    user.UserName = dto.Email;
                    user.NormalizedEmail = dto.Email.ToUpperInvariant();
                    user.NormalizedUserName = dto.Email.ToUpperInvariant();
                }
                
                // Update basic fields (for non-admin users, or if admin is updating non-admin)
                if (!targetIsAdmin)
                {
                    if (!string.IsNullOrEmpty(dto.FirstName))
                    {
                        user.FirstName = dto.FirstName;
                    }
                    if (!string.IsNullOrEmpty(dto.LastName))
                    {
                        user.LastName = dto.LastName;
                    }
                    // PhoneNumber can be null/empty, so we always update it
                    user.PhoneNumber = dto.PhoneNumber; // Can be null
                }
                else
                {
                    // For admin users, only email can be updated (restricted)
                    // But we still allow firstName/lastName if explicitly provided
                    if (!string.IsNullOrEmpty(dto.FirstName))
                    {
                        user.FirstName = dto.FirstName;
                    }
                    if (!string.IsNullOrEmpty(dto.LastName))
                    {
                        user.LastName = dto.LastName;
                    }
                    user.PhoneNumber = dto.PhoneNumber; // Can be null
                }
                
                user.UpdatedAt = DateTime.UtcNow;

                // Update user in Identity
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { 
                        message = "Kullanıcı güncellenirken hata oluştu", 
                        errors = result.Errors.Select(e => e.Description).ToList() 
                    });
                }

                // Handle role change if provided
                if (dto.Role != null)
                {
                    var newRole = dto.Role.Value;
                    string newRoleName = newRole switch
                    {
                        UserRole.Student => "Student",
                        UserRole.Faculty => "Faculty",
                        UserRole.Admin => "Admin",
                        _ => "Student"
                    };
                    
                    // Get current roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    
                    // Remove all existing roles
                    if (currentRoles.Any())
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            Console.WriteLine($"⚠️ Warning: Failed to remove roles from user {user.Id}: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    
                    // Add new role
                    var addRoleResult = await _userManager.AddToRoleAsync(user, newRoleName);
                    if (!addRoleResult.Succeeded)
                    {
                        Console.WriteLine($"⚠️ Warning: Failed to add role '{newRoleName}' to user {user.Id}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                        return BadRequest(new { 
                            message = $"Role '{newRoleName}' atanamadı", 
                            errors = addRoleResult.Errors.Select(e => e.Description).ToList() 
                        });
                    }
                    
                    Console.WriteLine($"✅ Role changed to '{newRoleName}' for user {user.Id} ({user.Email})");
                    
                    // If changing to Student or Faculty, ensure entry exists in respective table
                    if (newRole == UserRole.Student)
                    {
                        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (student == null && dto.DepartmentId.HasValue)
                        {
                            // Create Student entry if doesn't exist
                            var studentNumber = $"STU{user.Id:D6}"; // Generate student number
                            student = new Student
                            {
                                UserId = user.Id,
                                StudentNumber = studentNumber,
                                DepartmentId = dto.DepartmentId.Value,
                                GPA = 0,
                                CGPA = 0,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Students.Add(student);
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"✅ Created Student entry for user {user.Id}");
                        }
                    }
                    else if (newRole == UserRole.Faculty)
                    {
                        var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == user.Id);
                        if (faculty == null && dto.DepartmentId.HasValue)
                        {
                            // Create Faculty entry if doesn't exist
                            var employeeNumber = $"FAC{user.Id:D6}"; // Generate employee number
                            faculty = new Faculty
                            {
                                UserId = user.Id,
                                EmployeeNumber = employeeNumber,
                                DepartmentId = dto.DepartmentId.Value,
                                Title = "Öğretim Üyesi",
                                IsDeleted = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Faculties.Add(faculty);
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"✅ Created Faculty entry for user {user.Id}");
                        }
                    }
                    else if (newRole == UserRole.Admin)
                    {
                        // Remove Student/Faculty entries if changing to Admin
                        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (student != null)
                        {
                            _context.Students.Remove(student);
                        }
                        var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == user.Id);
                        if (faculty != null)
                        {
                            _context.Faculties.Remove(faculty);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { 
                    message = $"Kullanıcı {email} başarıyla güncellendi.", 
                    userId = user.Id 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating user {email}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    message = "Kullanıcı güncelleme sırasında hata oluştu", 
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Kullanıcı şifresini sıfırla (Admin only)
        /// </summary>
        [HttpPost("users/reset-password/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetUserPassword(string email, [FromBody] AdminResetPasswordDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı.", email });
                }

                // Remove old password
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    return BadRequest(new { message = "Eski şifre kaldırılamadı", errors = removePasswordResult.Errors.Select(e => e.Description) });
                }

                // Add new password
                var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                if (addPasswordResult.Succeeded)
                {
                    // Unlock account if locked
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                    
                    return Ok(new { message = $"Kullanıcı {email} şifresi başarıyla sıfırlandı." });
                }
                else
                {
                    return BadRequest(new { message = "Yeni şifre eklenemedi", errors = addPasswordResult.Errors.Select(e => e.Description) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şifre sıfırlama sırasında hata oluştu", error = ex.Message });
            }
        }
    }

    public class AdminResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Public endpoint to create admin user (Development only - should be removed in production)
    /// </summary>
    [Route("api/v1/admin")]
    [ApiController]
    [AllowAnonymous] // Public endpoint for initial admin creation
    public class AdminSetupController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<AdminSetupController> _logger;

        public AdminSetupController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILogger<AdminSetupController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Check if admin user exists (Public endpoint)
        /// </summary>
        [HttpGet("check-admin")]
        public async Task<IActionResult> CheckAdmin()
        {
            try
            {
                var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@smartcampus.edu";
                var adminUser = await _userManager.FindByEmailAsync(adminEmail);
                
                if (adminUser == null)
                {
                    return Ok(new { 
                        exists = false, 
                        message = "Admin user does not exist",
                        email = adminEmail 
                    });
                }

                var isAdmin = await _userManager.IsInRoleAsync(adminUser, "Admin");
                var emailConfirmed = await _userManager.IsEmailConfirmedAsync(adminUser);
                var isLockedOut = await _userManager.IsLockedOutAsync(adminUser);

                return Ok(new
                {
                    exists = true,
                    email = adminUser.Email,
                    userId = adminUser.Id,
                    isAdmin,
                    emailConfirmed,
                    isLockedOut,
                    firstName = adminUser.FirstName,
                    lastName = adminUser.LastName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking admin user", error = ex.Message });
            }
        }

        /// <summary>
        /// Create admin user (Public endpoint - Development only)
        /// </summary>
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
        {
            try
            {
                // Check if admin already exists
                var existingAdmin = await _userManager.FindByEmailAsync(dto.Email);
                if (existingAdmin != null)
                {
                    // Check if user has Admin role
                    var isAdmin = await _userManager.IsInRoleAsync(existingAdmin, "Admin");
                    if (isAdmin)
                    {
                        return BadRequest(new { message = $"Admin user with email {dto.Email} already exists." });
                    }
                    else
                    {
                        // User exists but not admin - add admin role
                        var roleResult = await _userManager.AddToRoleAsync(existingAdmin, "Admin");
                        if (roleResult.Succeeded)
                        {
                            // Reset password
                            var token = await _userManager.GeneratePasswordResetTokenAsync(existingAdmin);
                            var resetResult = await _userManager.ResetPasswordAsync(existingAdmin, token, dto.Password);
                            if (resetResult.Succeeded)
                            {
                                existingAdmin.EmailConfirmed = true;
                                await _userManager.UpdateAsync(existingAdmin);
                                return Ok(new { message = $"Admin role added to existing user. Password reset.", email = dto.Email });
                            }
                        }
                    }
                }

                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var role = new Role { Name = "Admin" };
                    await _roleManager.CreateAsync(role);
                }

                // Create new admin user
                var adminUser = new User
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName ?? "Admin",
                    LastName = dto.LastName ?? "User",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(adminUser, dto.Password);
                if (!createResult.Succeeded)
                {
                    return BadRequest(new { 
                        message = "Failed to create admin user", 
                        errors = createResult.Errors.Select(e => e.Description) 
                    });
                }

                // Assign Admin role
                var addRoleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    // Rollback user creation
                    await _userManager.DeleteAsync(adminUser);
                    return BadRequest(new { 
                        message = "Failed to assign Admin role", 
                        errors = addRoleResult.Errors.Select(e => e.Description) 
                    });
                }

                _logger.LogInformation($"✅ Admin user created: {dto.Email}");
                return Ok(new { 
                    message = "Admin user created successfully", 
                    email = dto.Email,
                    userId = adminUser.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user");
                return StatusCode(500, new { message = "Error creating admin user", error = ex.Message });
            }
        }
    }

    public class CreateAdminDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}

