using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface ICourseService
    {
        Task<PaginatedResponse<CourseDto>> GetCoursesAsync(int page, int limit, string? search, int? departmentId, string? sort);
        Task<CourseDto?> GetCourseByIdAsync(int id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseDto?> UpdateCourseAsync(int id, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int id);
        Task<List<CourseSectionDto>> GetSectionsAsync(string? semester, int? year, int? instructorId, int? courseId);
        Task<CourseSectionDto?> GetSectionByIdAsync(int id);
        Task<CourseSectionDto> CreateSectionAsync(CreateSectionDto dto);
        Task<CourseSectionDto?> UpdateSectionAsync(int id, UpdateSectionDto dto);
        Task<bool> DeleteSectionAsync(int id);
    }

    public class CourseService : ICourseService
    {
        private readonly CampusDbContext _context;

        public CourseService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResponse<CourseDto>> GetCoursesAsync(int page, int limit, string? search, int? departmentId, string? sort)
        {
            var query = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .Where(c => !c.IsDeleted);

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c => c.Code.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
            }

            // Filter by department
            if (departmentId.HasValue)
            {
                query = query.Where(c => c.DepartmentId == departmentId.Value);
            }

            // Sort
            query = sort?.ToLower() switch
            {
                "name" => query.OrderBy(c => c.Name),
                "code" => query.OrderBy(c => c.Code),
                "credits" => query.OrderByDescending(c => c.Credits),
                _ => query.OrderBy(c => c.Code)
            };

            var total = await query.CountAsync();
            var courses = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return new PaginatedResponse<CourseDto>
            {
                Data = (await Task.WhenAll(courses.Select(c => MapToCourseDtoAsync(c)))).ToList(),
                Pagination = new PaginationInfo
                {
                    Page = page,
                    Limit = limit,
                    Total = total
                }
            };
        }

        public async Task<CourseDto?> GetCourseByIdAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .Include(c => c.Sections.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            return course == null ? null : await MapToCourseDtoAsync(course, includeSections: true);
        }

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            var course = new Course
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Credits = dto.Credits,
                Ects = dto.Ects,
                SyllabusUrl = dto.SyllabusUrl,
                DepartmentId = dto.DepartmentId,
                Type = dto.Type,
                AllowCrossDepartment = dto.AllowCrossDepartment
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Add prerequisites
            if (dto.PrerequisiteIds?.Any() == true)
            {
                foreach (var prereqId in dto.PrerequisiteIds)
                {
                    _context.CoursePrerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = course.Id,
                        PrerequisiteCourseId = prereqId
                    });
                }
                await _context.SaveChangesAsync();
            }

            var createdCourse = await GetCourseByIdAsync(course.Id);
            return createdCourse ?? await MapToCourseDtoAsync(course);
        }

        public async Task<CourseDto?> UpdateCourseAsync(int id, UpdateCourseDto dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.IsDeleted) return null;

            if (dto.Name != null) course.Name = dto.Name;
            if (dto.Description != null) course.Description = dto.Description;
            if (dto.Credits.HasValue) course.Credits = dto.Credits.Value;
            if (dto.Ects.HasValue) course.Ects = dto.Ects.Value;
            if (dto.SyllabusUrl != null) course.SyllabusUrl = dto.SyllabusUrl;
            if (dto.Type.HasValue) course.Type = dto.Type.Value;
            if (dto.AllowCrossDepartment.HasValue) course.AllowCrossDepartment = dto.AllowCrossDepartment.Value;
            course.UpdatedAt = DateTime.UtcNow;

            // Update prerequisites
            if (dto.PrerequisiteIds != null)
            {
                var existing = await _context.CoursePrerequisites.Where(cp => cp.CourseId == id).ToListAsync();
                _context.CoursePrerequisites.RemoveRange(existing);

                foreach (var prereqId in dto.PrerequisiteIds)
                {
                    _context.CoursePrerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = id,
                        PrerequisiteCourseId = prereqId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GetCourseByIdAsync(id);
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.IsDeleted) return false;

            course.IsDeleted = true;
            course.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== SECTIONS ====================

        public async Task<List<CourseSectionDto>> GetSectionsAsync(string? semester, int? year, int? instructorId, int? courseId)
        {
            var query = _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Classroom)
                .Where(s => !s.IsDeleted);

            if (!string.IsNullOrEmpty(semester))
                query = query.Where(s => s.Semester == semester);
            if (year.HasValue)
                query = query.Where(s => s.Year == year.Value);
            if (instructorId.HasValue)
                query = query.Where(s => s.InstructorId == instructorId.Value);
            if (courseId.HasValue)
                query = query.Where(s => s.CourseId == courseId.Value);

            var sections = await query.ToListAsync();
            var sectionDtos = new List<CourseSectionDto>();
            foreach (var section in sections)
            {
                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.SectionId == section.Id && e.Status == "enrolled");
                sectionDtos.Add(await MapToSectionDtoAsync(section, enrolledCount));
            }
            return sectionDtos;
        }

        public async Task<CourseSectionDto?> GetSectionByIdAsync(int id)
        {
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (section == null) return null;
            var enrolledCount = await _context.Enrollments
                .CountAsync(e => e.SectionId == section.Id && e.Status == "enrolled");
            return await MapToSectionDtoAsync(section, enrolledCount);
        }

        public async Task<CourseSectionDto> CreateSectionAsync(CreateSectionDto dto)
        {
            var section = new CourseSection
            {
                CourseId = dto.CourseId,
                SectionNumber = dto.SectionNumber,
                Semester = dto.Semester,
                Year = dto.Year,
                InstructorId = dto.InstructorId,
                Capacity = dto.Capacity,
                ScheduleJson = dto.ScheduleJson,
                ClassroomId = dto.ClassroomId
            };

            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            var enrolledCount = await _context.Enrollments
                .CountAsync(e => e.SectionId == section.Id && e.Status == "enrolled");
            return await MapToSectionDtoAsync(section, enrolledCount);
        }

        public async Task<CourseSectionDto?> UpdateSectionAsync(int id, UpdateSectionDto dto)
        {
            var section = await _context.CourseSections.FindAsync(id);
            if (section == null || section.IsDeleted) return null;

            if (dto.InstructorId.HasValue) section.InstructorId = dto.InstructorId.Value;
            if (dto.Capacity.HasValue) section.Capacity = dto.Capacity.Value;
            if (dto.ScheduleJson != null) section.ScheduleJson = dto.ScheduleJson;
            if (dto.ClassroomId.HasValue) section.ClassroomId = dto.ClassroomId.Value;
            section.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetSectionByIdAsync(id);
        }

        public async Task<bool> DeleteSectionAsync(int id)
        {
            var section = await _context.CourseSections.FindAsync(id);
            if (section == null || section.IsDeleted) return false;

            section.IsDeleted = true;
            section.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== MAPPERS ====================

        private async Task<CourseDto> MapToCourseDtoAsync(Course course, bool includeSections = false)
        {
            return new CourseDto
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Description = course.Description,
                Credits = course.Credits,
                Ects = course.Ects,
                SyllabusUrl = course.SyllabusUrl,
                Type = course.Type,
                AllowCrossDepartment = course.AllowCrossDepartment,
                Department = course.Department == null ? null : new DepartmentDto
                {
                    Id = course.Department.Id,
                    Name = course.Department.Name,
                    Code = course.Department.Code,
                    FacultyName = course.Department.FacultyName
                },
                Prerequisites = course.Prerequisites?.Select(p => new CourseDto
                {
                    Id = p.PrerequisiteCourse?.Id ?? 0,
                    Code = p.PrerequisiteCourse?.Code ?? "",
                    Name = p.PrerequisiteCourse?.Name ?? ""
                }).ToList(),
                Sections = includeSections ? await MapSectionsToDtoAsync(course.Sections) : null
            };
        }

        private async Task<CourseSectionDto> MapToSectionDtoAsync(CourseSection section, int enrolledCount)
        {
            return new CourseSectionDto
            {
                Id = section.Id,
                CourseId = section.CourseId,
                CourseCode = section.Course?.Code ?? "",
                CourseName = section.Course?.Name ?? "",
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
                Classroom = section.Classroom == null ? null : new ClassroomDto
                {
                    Id = section.Classroom.Id,
                    Building = section.Classroom.Building,
                    RoomNumber = section.Classroom.RoomNumber,
                    Capacity = section.Classroom.Capacity,
                    Latitude = section.Classroom.Latitude,
                    Longitude = section.Classroom.Longitude,
                    FeaturesJson = section.Classroom.FeaturesJson
                }
            };
        }

        private async Task<List<CourseSectionDto>> MapSectionsToDtoAsync(IEnumerable<CourseSection>? sections)
        {
            if (sections == null) return new List<CourseSectionDto>();
            
            var sectionDtos = new List<CourseSectionDto>();
            foreach (var section in sections)
            {
                var enrolledCount = await _context.Enrollments
                    .CountAsync(e => e.SectionId == section.Id && e.Status == "enrolled");
                sectionDtos.Add(await MapToSectionDtoAsync(section, enrolledCount));
            }
            return sectionDtos;
        }
    }
}
