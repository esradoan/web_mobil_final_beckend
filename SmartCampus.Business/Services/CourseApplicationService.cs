using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface ICourseApplicationService
    {
        Task<CourseApplicationDto> CreateApplicationAsync(int courseId, int instructorId);
        Task<ApplicationListResponseDto> GetApplicationsAsync(int? instructorId, ApplicationStatus? status, int page = 1, int pageSize = 10);
        Task<CourseApplicationDto?> GetApplicationByIdAsync(int id);
        Task<CourseApplicationDto> ApproveApplicationAsync(int applicationId, int adminUserId);
        Task<CourseApplicationDto> RejectApplicationAsync(int applicationId, int adminUserId, string? reason = null);
        Task<bool> CanInstructorApplyAsync(int instructorId, int courseId);
    }

    public class CourseApplicationService : ICourseApplicationService
    {
        private readonly CampusDbContext _context;
        private const int MAX_COURSES_PER_INSTRUCTOR = 2;

        public CourseApplicationService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<CourseApplicationDto> CreateApplicationAsync(int courseId, int instructorId)
        {
            // Course var mı kontrol et
            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null)
                throw new Exception("Ders bulunamadı.");

            // Öğretmen var mı ve Faculty mi kontrol et
            var faculty = await _context.Faculties
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.UserId == instructorId);

            if (faculty == null)
                throw new Exception("Öğretmen bulunamadı.");

            // Aynı course'a daha önce başvuru yapılmış mı?
            var existingApplication = await _context.CourseApplications
                .FirstOrDefaultAsync(a => a.CourseId == courseId && a.InstructorId == instructorId);

            if (existingApplication != null)
            {
                if (existingApplication.Status == ApplicationStatus.Pending)
                    throw new Exception("Bu derse zaten başvuru yaptınız ve başvurunuz beklemektedir.");
                if (existingApplication.Status == ApplicationStatus.Approved)
                    throw new Exception("Bu derse zaten atanmışsınız.");
                if (existingApplication.Status == ApplicationStatus.Rejected)
                    throw new Exception("Bu derse yaptığınız başvuru reddedilmiştir.");
            }

            // Bu course'a başka bir öğretmen atanmış mı? (Section'larda kontrol)
            var existingSections = await _context.CourseSections
                .Include(s => s.Instructor)
                .Where(s => s.CourseId == courseId && s.InstructorId > 0 && !s.IsDeleted)
                .ToListAsync();

            if (existingSections.Any())
            {
                var assignedInstructor = existingSections.First().Instructor;
                if (assignedInstructor != null && assignedInstructor.Id != instructorId)
                {
                    throw new Exception($"Bu dersi {assignedInstructor.FirstName} {assignedInstructor.LastName} vermektedir. Başvuru yapılamaz.");
                }
            }

            // Onaylanmış başvuru var mı?
            var approvedApplication = await _context.CourseApplications
                .Include(a => a.Instructor)
                .FirstOrDefaultAsync(a => a.CourseId == courseId && a.Status == ApplicationStatus.Approved);

            if (approvedApplication != null && approvedApplication.InstructorId != instructorId)
            {
                var instructorName = approvedApplication.Instructor != null 
                    ? $"{approvedApplication.Instructor.FirstName} {approvedApplication.Instructor.LastName}" 
                    : "Bir öğretmen";
                throw new Exception($"Bu dersi {instructorName} vermektedir. Başvuru yapılamaz.");
            }

            // Öğretmenin maksimum 2 ders limiti kontrolü
            // Onaylanmış başvuruları say
            var approvedApplications = await _context.CourseApplications
                .CountAsync(a => a.InstructorId == instructorId && a.Status == ApplicationStatus.Approved);

            // Öğretmenin verdiği dersleri say (section'lardan)
            var instructorCourses = await _context.CourseSections
                .Where(s => s.InstructorId == instructorId && !s.IsDeleted)
                .Select(s => s.CourseId)
                .Distinct()
                .CountAsync();

            var totalCourses = approvedApplications + instructorCourses;

            if (totalCourses >= MAX_COURSES_PER_INSTRUCTOR)
                throw new Exception($"Maksimum {MAX_COURSES_PER_INSTRUCTOR} ders alabilirsiniz. Şu anda {totalCourses} dersiniz bulunmaktadır.");

            // Başvuru oluştur
            var application = new CourseApplication
            {
                CourseId = courseId,
                InstructorId = instructorId,
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseApplications.Add(application);
            await _context.SaveChangesAsync();

            return await MapToDtoAsync(application);
        }

        public async Task<ApplicationListResponseDto> GetApplicationsAsync(int? instructorId, ApplicationStatus? status, int page = 1, int pageSize = 10)
        {
            var query = _context.CourseApplications
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Instructor)
                .Include(a => a.ProcessedByUser)
                .AsQueryable();

            // Öğretmen filtresi
            if (instructorId.HasValue)
            {
                query = query.Where(a => a.InstructorId == instructorId.Value);
            }

            // Status filtresi
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var total = await query.CountAsync();

            var applications = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var applicationDtos = new List<CourseApplicationDto>();
            foreach (var app in applications)
            {
                applicationDtos.Add(await MapToDtoAsync(app));
            }

            return new ApplicationListResponseDto
            {
                Data = applicationDtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CourseApplicationDto?> GetApplicationByIdAsync(int id)
        {
            var application = await _context.CourseApplications
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Instructor)
                .Include(a => a.ProcessedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return null;

            return await MapToDtoAsync(application);
        }

        public async Task<CourseApplicationDto> ApproveApplicationAsync(int applicationId, int adminUserId)
        {
            var application = await _context.CourseApplications
                .Include(a => a.Course)
                .Include(a => a.Instructor)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                throw new Exception("Başvuru bulunamadı.");

            if (application.Status != ApplicationStatus.Pending)
                throw new Exception("Sadece bekleyen başvurular onaylanabilir.");

            // Course'a ait section var mı kontrol et, yoksa otomatik oluştur
            var existingSections = await _context.CourseSections
                .Where(s => s.CourseId == application.CourseId && !s.IsDeleted)
                .ToListAsync();

            CourseSection section;
            if (!existingSections.Any())
            {
                // Section yoksa otomatik oluştur
                // Mevcut dönem ve yıl bilgisini al (şimdilik default)
                var currentYear = DateTime.Now.Year;
                var currentSemester = DateTime.Now.Month >= 9 ? "Fall" : "Spring"; // Eylül'den sonra Fall, değilse Spring

                section = new CourseSection
                {
                    CourseId = application.CourseId,
                    SectionNumber = "01", // İlk section
                    Semester = currentSemester,
                    Year = currentYear,
                    InstructorId = application.InstructorId,
                    Capacity = 50, // Default capacity
                    EnrolledCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CourseSections.Add(section);
            }
            else
            {
                // Section varsa, instructor'ı atanmamış bir section'a ata veya yeni section oluştur
                var unassignedSection = existingSections.FirstOrDefault(s => s.InstructorId == 0);
                
                if (unassignedSection != null)
                {
                    section = unassignedSection;
                    section.InstructorId = application.InstructorId;
                }
                else
                {
                    // Tüm section'lar atanmış, yeni section oluştur
                    var maxSectionNumber = existingSections
                        .Select(s => int.TryParse(s.SectionNumber, out var num) ? num : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    var currentYear = DateTime.Now.Year;
                    var currentSemester = DateTime.Now.Month >= 9 ? "Fall" : "Spring";

                    section = new CourseSection
                    {
                        CourseId = application.CourseId,
                        SectionNumber = (maxSectionNumber + 1).ToString("D2"),
                        Semester = currentSemester,
                        Year = currentYear,
                        InstructorId = application.InstructorId,
                        Capacity = 50,
                        EnrolledCount = 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CourseSections.Add(section);
                }
            }

            // Bu course'a ait diğer pending başvuruları reddet
            var otherPendingApplications = await _context.CourseApplications
                .Where(a => a.CourseId == application.CourseId && 
                           a.Id != applicationId && 
                           a.Status == ApplicationStatus.Pending)
                .ToListAsync();

            foreach (var otherApp in otherPendingApplications)
            {
                otherApp.Status = ApplicationStatus.Rejected;
                otherApp.ProcessedAt = DateTime.UtcNow;
                otherApp.ProcessedBy = adminUserId;
                otherApp.RejectionReason = "Başka bir öğretmen bu derse atandı.";
            }

            // Başvuruyu onayla
            application.Status = ApplicationStatus.Approved;
            application.ProcessedAt = DateTime.UtcNow;
            application.ProcessedBy = adminUserId;

            await _context.SaveChangesAsync();

            return await MapToDtoAsync(application);
        }

        public async Task<CourseApplicationDto> RejectApplicationAsync(int applicationId, int adminUserId, string? reason = null)
        {
            var application = await _context.CourseApplications
                .Include(a => a.Course)
                .Include(a => a.Instructor)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                throw new Exception("Başvuru bulunamadı.");

            if (application.Status != ApplicationStatus.Pending)
                throw new Exception("Sadece bekleyen başvurular reddedilebilir.");

            application.Status = ApplicationStatus.Rejected;
            application.ProcessedAt = DateTime.UtcNow;
            application.ProcessedBy = adminUserId;
            application.RejectionReason = reason ?? "Başvuru reddedildi.";

            await _context.SaveChangesAsync();

            return await MapToDtoAsync(application);
        }

        public async Task<bool> CanInstructorApplyAsync(int instructorId, int courseId)
        {
            // Course var mı?
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null)
                return false;

            // Zaten onaylanmış başvuru var mı?
            var approvedApplication = await _context.CourseApplications
                .AnyAsync(a => a.CourseId == courseId && a.Status == ApplicationStatus.Approved);

            if (approvedApplication)
            {
                // Kendi başvurusu mu kontrol et
                var myApprovedApp = await _context.CourseApplications
                    .FirstOrDefaultAsync(a => a.CourseId == courseId && 
                                              a.InstructorId == instructorId && 
                                              a.Status == ApplicationStatus.Approved);
                if (myApprovedApp == null)
                    return false; // Başka biri atanmış
            }

            // Öğretmenin ders limiti
            var approvedApplications = await _context.CourseApplications
                .CountAsync(a => a.InstructorId == instructorId && a.Status == ApplicationStatus.Approved);

            var instructorCourses = await _context.CourseSections
                .Where(s => s.InstructorId == instructorId && !s.IsDeleted)
                .Select(s => s.CourseId)
                .Distinct()
                .CountAsync();

            if (approvedApplications + instructorCourses >= MAX_COURSES_PER_INSTRUCTOR)
                return false;

            return true;
        }

        private async Task<CourseApplicationDto> MapToDtoAsync(CourseApplication application)
        {
            // Course bilgilerini yükle
            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == application.CourseId);

            var dto = new CourseApplicationDto
            {
                Id = application.Id,
                CourseId = application.CourseId,
                InstructorId = application.InstructorId,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                ProcessedAt = application.ProcessedAt,
                ProcessedBy = application.ProcessedBy,
                RejectionReason = application.RejectionReason,
                InstructorName = application.Instructor != null 
                    ? $"{application.Instructor.FirstName} {application.Instructor.LastName}" 
                    : "",
                InstructorEmail = application.Instructor?.Email ?? ""
            };

            // Course DTO
            if (course != null)
            {
                dto.Course = new CourseDto
                {
                    Id = course.Id,
                    Code = course.Code,
                    Name = course.Name,
                    Description = course.Description,
                    Credits = course.Credits,
                    Ects = course.Ects,
                    DepartmentId = course.DepartmentId,
                    Type = course.Type,
                    Department = course.Department != null ? new DepartmentDto
                    {
                        Id = course.Department.Id,
                        Name = course.Department.Name,
                        Code = course.Department.Code,
                        FacultyName = course.Department.FacultyName
                    } : null
                };
            }

            // ProcessedBy bilgisi
            if (application.ProcessedBy.HasValue)
            {
                var processedByUser = await _context.Users.FindAsync(application.ProcessedBy.Value);
                dto.ProcessedByName = processedByUser != null 
                    ? $"{processedByUser.FirstName} {processedByUser.LastName}" 
                    : "";
            }

            return dto;
        }
    }
}

