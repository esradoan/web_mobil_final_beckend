using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IStudentCourseApplicationService
    {
        Task<StudentCourseApplicationDto> CreateApplicationAsync(int courseId, int sectionId, int studentId);
        Task<StudentApplicationListResponseDto> GetApplicationsAsync(int? studentId, ApplicationStatus? status, int page = 1, int pageSize = 10);
        Task<StudentCourseApplicationDto?> GetApplicationByIdAsync(int id);
        Task<StudentCourseApplicationDto> ApproveApplicationAsync(int applicationId, int adminUserId);
        Task<StudentCourseApplicationDto> RejectApplicationAsync(int applicationId, int adminUserId, string? reason = null);
        Task<bool> CanStudentApplyAsync(int studentId, int sectionId);
        Task<List<CourseDto>> GetAvailableCoursesForStudentAsync(int studentId);
    }

    public class StudentCourseApplicationService : IStudentCourseApplicationService
    {
        private readonly CampusDbContext _context;

        public StudentCourseApplicationService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<StudentCourseApplicationDto> CreateApplicationAsync(int courseId, int sectionId, int studentId)
        {
            // Course ve Section var mı kontrol et
            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null)
                throw new Exception("Ders bulunamadı.");

            var section = await _context.CourseSections
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == sectionId && s.CourseId == courseId && !s.IsDeleted);

            if (section == null)
                throw new Exception("Şube bulunamadı veya bu derse ait değil.");

            // Öğrenci var mı kontrol et
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (student == null)
                throw new Exception("Öğrenci bulunamadı.");

            // Aynı section'a daha önce başvuru yapılmış mı?
            var existingApplication = await _context.StudentCourseApplications
                .FirstOrDefaultAsync(a => a.SectionId == sectionId && a.StudentId == studentId);

            if (existingApplication != null)
            {
                if (existingApplication.Status == ApplicationStatus.Pending)
                    throw new Exception("Bu şubeye zaten başvuru yaptınız ve başvurunuz beklemektedir.");
                if (existingApplication.Status == ApplicationStatus.Approved)
                    throw new Exception("Bu şubeye zaten kayıtlısınız.");
                if (existingApplication.Status == ApplicationStatus.Rejected)
                    throw new Exception("Bu şubeye yaptığınız başvuru reddedilmiştir.");
            }

            // Zaten enrollment var mı?
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.SectionId == sectionId && e.StudentId == studentId);

            if (existingEnrollment != null)
                throw new Exception("Bu şubeye zaten kayıtlısınız.");

            // Section kapasitesi kontrolü
            var currentEnrollments = await _context.Enrollments
                .CountAsync(e => e.SectionId == sectionId && e.Status == "enrolled");

            if (currentEnrollments >= section.Capacity)
                throw new Exception("Bu şubenin kapasitesi dolmuştur.");

            // Başvuru oluştur
            var application = new StudentCourseApplication
            {
                CourseId = courseId,
                SectionId = sectionId,
                StudentId = studentId,
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentCourseApplications.Add(application);
            await _context.SaveChangesAsync();

            return await MapToDtoAsync(application);
        }

        public async Task<StudentApplicationListResponseDto> GetApplicationsAsync(int? studentId, ApplicationStatus? status, int page = 1, int pageSize = 10)
        {
            var query = _context.StudentCourseApplications
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Section)
                    .ThenInclude(s => s.Course)
                .Include(a => a.Student)
                .Include(a => a.ProcessedByUser)
                .AsQueryable();

            // Öğrenci filtresi
            if (studentId.HasValue)
            {
                query = query.Where(a => a.StudentId == studentId.Value);
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

            var applicationDtos = new List<StudentCourseApplicationDto>();
            foreach (var app in applications)
            {
                applicationDtos.Add(await MapToDtoAsync(app));
            }

            return new StudentApplicationListResponseDto
            {
                Data = applicationDtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<StudentCourseApplicationDto?> GetApplicationByIdAsync(int id)
        {
            var application = await _context.StudentCourseApplications
                .Include(a => a.Course)
                    .ThenInclude(c => c.Department)
                .Include(a => a.Section)
                    .ThenInclude(s => s.Course)
                .Include(a => a.Student)
                .Include(a => a.ProcessedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return null;

            return await MapToDtoAsync(application);
        }

        public async Task<StudentCourseApplicationDto> ApproveApplicationAsync(int applicationId, int adminUserId)
        {
            var application = await _context.StudentCourseApplications
                .Include(a => a.Section)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                throw new Exception("Başvuru bulunamadı.");

            if (application.Status != ApplicationStatus.Pending)
                throw new Exception("Sadece bekleyen başvurular onaylanabilir.");

            // Section kapasitesi kontrolü
            var currentEnrollments = await _context.Enrollments
                .CountAsync(e => e.SectionId == application.SectionId && e.Status == "enrolled");

            if (currentEnrollments >= application.Section!.Capacity)
                throw new Exception("Bu şubenin kapasitesi dolmuştur.");

            // Enrollment oluştur
            // application.StudentId userId'dir, Student entity'sinin Id'sini bul
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == application.StudentId);
            
            if (student == null)
                throw new Exception("Öğrenci bulunamadı.");

            var enrollment = new Enrollment
            {
                StudentId = student.Id, // Student entity'sinin Id'si, userId değil!
                SectionId = application.SectionId,
                Status = "enrolled",
                EnrollmentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);

            // Başvuruyu onayla
            application.Status = ApplicationStatus.Approved;
            application.ProcessedAt = DateTime.UtcNow;
            application.ProcessedBy = adminUserId;

            await _context.SaveChangesAsync();

            return await MapToDtoAsync(application);
        }

        public async Task<StudentCourseApplicationDto> RejectApplicationAsync(int applicationId, int adminUserId, string? reason = null)
        {
            var application = await _context.StudentCourseApplications
                .Include(a => a.Course)
                .Include(a => a.Student)
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

        public async Task<bool> CanStudentApplyAsync(int studentId, int sectionId)
        {
            // Section var mı?
            var section = await _context.CourseSections
                .FirstOrDefaultAsync(s => s.Id == sectionId && !s.IsDeleted);

            if (section == null)
                return false;

            // Zaten başvuru var mı?
            var existingApplication = await _context.StudentCourseApplications
                .AnyAsync(a => a.SectionId == sectionId && a.StudentId == studentId && a.Status == ApplicationStatus.Pending);

            if (existingApplication)
                return false;

            // Zaten enrollment var mı?
            var existingEnrollment = await _context.Enrollments
                .AnyAsync(e => e.SectionId == sectionId && e.StudentId == studentId);

            if (existingEnrollment)
                return false;

            // Kapasite kontrolü
            var currentEnrollments = await _context.Enrollments
                .CountAsync(e => e.SectionId == sectionId && e.Status == "enrolled");

            if (currentEnrollments >= section.Capacity)
                return false;

            return true;
        }

        public async Task<List<CourseDto>> GetAvailableCoursesForStudentAsync(int studentId)
        {
            // Öğrencinin bölümünü al
            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (student == null)
                return new List<CourseDto>();

            // Tüm dersleri al (hocası olanlar önce)
            var allCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Sections.Where(s => !s.IsDeleted && s.InstructorId > 0))
                    .ThenInclude(s => s.Instructor)
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            // Hocası olan dersleri önce göster
            var coursesWithInstructors = allCourses
                .Where(c => c.Sections != null && c.Sections.Any(s => s.InstructorId > 0))
                .OrderByDescending(c => c.Sections!.Count(s => s.InstructorId > 0))
                .ToList();

            var coursesWithoutInstructors = allCourses
                .Where(c => c.Sections == null || !c.Sections.Any(s => s.InstructorId > 0))
                .ToList();

            var orderedCourses = coursesWithInstructors.Concat(coursesWithoutInstructors).ToList();

            // DTO'ya çevir
            var courseDtos = new List<CourseDto>();
            foreach (var course in orderedCourses)
            {
                var sections = await _context.CourseSections
                    .Include(s => s.Instructor)
                    .Where(s => s.CourseId == course.Id && !s.IsDeleted)
                    .ToListAsync();

                var sectionDtos = new List<CourseSectionDto>();
                foreach (var s in sections)
                {
                    var enrolledCount = await _context.Enrollments
                        .CountAsync(e => e.SectionId == s.Id && e.Status == "enrolled");
                    
                    sectionDtos.Add(new CourseSectionDto
                    {
                        Id = s.Id,
                        CourseId = s.CourseId,
                        CourseCode = course.Code,
                        CourseName = course.Name,
                        SectionNumber = s.SectionNumber,
                        Semester = s.Semester,
                        Year = s.Year,
                        InstructorId = s.InstructorId,
                        InstructorName = s.Instructor != null 
                            ? $"{s.Instructor.FirstName} {s.Instructor.LastName}" 
                            : "",
                        Capacity = s.Capacity,
                        EnrolledCount = enrolledCount,
                        ScheduleJson = s.ScheduleJson,
                        ClassroomId = s.ClassroomId
                    });
                }

                courseDtos.Add(new CourseDto
                {
                    Id = course.Id,
                    Code = course.Code,
                    Name = course.Name,
                    Description = course.Description,
                    Credits = course.Credits,
                    Ects = course.Ects,
                    DepartmentId = course.DepartmentId,
                    Department = course.Department != null ? new DepartmentDto
                    {
                        Id = course.Department.Id,
                        Name = course.Department.Name,
                        Code = course.Department.Code,
                        FacultyName = course.Department.FacultyName
                    } : null,
                    Type = course.Type,
                    Sections = sectionDtos
                });
            }

            return courseDtos;
        }

        private async Task<StudentCourseApplicationDto> MapToDtoAsync(StudentCourseApplication application)
        {
            // Course bilgilerini yükle
            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == application.CourseId);

            // Section bilgilerini yükle
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id == application.SectionId);

            // Student bilgilerini yükle (Student entity'den StudentNumber almak için)
            // Student bilgilerini yükle (Student entity'den StudentNumber almak için)
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == application.StudentId);

            // User bilgisi (application.Student zaten User navigation property)
            var user = application.Student ?? await _context.Users.FindAsync(application.StudentId);

            var dto = new StudentCourseApplicationDto
            {
                Id = application.Id,
                CourseId = application.CourseId,
                SectionId = application.SectionId,
                StudentId = application.StudentId,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                ProcessedAt = application.ProcessedAt,
                ProcessedBy = application.ProcessedBy,
                RejectionReason = application.RejectionReason,
                StudentName = user != null 
                    ? $"{user.FirstName} {user.LastName}" 
                    : (student?.User != null 
                        ? $"{student.User.FirstName} {student.User.LastName}" 
                        : ""),
                StudentEmail = user?.Email ?? student?.User?.Email ?? "",
                StudentNumber = student?.StudentNumber ?? ""
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

            // Section DTO
            if (section != null)
            {
                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.SectionId == section.Id && e.Status == "enrolled");

                dto.Section = new CourseSectionDto
                {
                    Id = section.Id,
                    CourseId = section.CourseId,
                    CourseCode = section.Course?.Code ?? course?.Code ?? "",
                    CourseName = section.Course?.Name ?? course?.Name ?? "",
                    SectionNumber = section.SectionNumber,
                    Semester = section.Semester,
                    Year = section.Year,
                    InstructorId = section.InstructorId,
                    InstructorName = section.Instructor != null 
                        ? $"{section.Instructor.FirstName} {section.Instructor.LastName}" 
                        : "",
                    Capacity = section.Capacity,
                    EnrolledCount = enrolledCount,
                    ScheduleJson = section.ScheduleJson,
                    ClassroomId = section.ClassroomId
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

