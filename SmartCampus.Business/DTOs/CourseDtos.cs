using SmartCampus.Entities;

namespace SmartCampus.Business.DTOs
{
    // ==================== COURSE DTOs ====================
    
    public class CourseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Ects { get; set; }
        public string? SyllabusUrl { get; set; }
        public CourseType Type { get; set; } = CourseType.Required;
        public bool AllowCrossDepartment { get; set; } = false;
        public int? DepartmentId { get; set; }
        public DepartmentDto? Department { get; set; }
        public List<CourseDto>? Prerequisites { get; set; }
        public List<CourseSectionDto>? Sections { get; set; }
    }

    public class CreateCourseDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Ects { get; set; }
        public string? SyllabusUrl { get; set; }
        public CourseType Type { get; set; } = CourseType.Required;
        public bool AllowCrossDepartment { get; set; } = false;
        public int DepartmentId { get; set; }
        public List<int>? PrerequisiteIds { get; set; }
    }

    public class UpdateCourseDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Credits { get; set; }
        public int? Ects { get; set; }
        public string? SyllabusUrl { get; set; }
        public CourseType? Type { get; set; }
        public bool? AllowCrossDepartment { get; set; }
        public List<int>? PrerequisiteIds { get; set; }
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
    }

    // ==================== COURSE SECTION DTOs ====================
    
    public class CourseSectionDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string SectionNumber { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public int Year { get; set; }
        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public int AvailableSeats => Capacity - EnrolledCount;
        public string? ScheduleJson { get; set; }
        public int? ClassroomId { get; set; }
        public ClassroomDto? Classroom { get; set; }
        public CourseDto? Course { get; set; }
    }

    public class CreateSectionDto
    {
        public int CourseId { get; set; }
        public string SectionNumber { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public int Year { get; set; }
        public int InstructorId { get; set; }
        public int Capacity { get; set; } = 50;
        public string? ScheduleJson { get; set; }
        public int? ClassroomId { get; set; }
    }

    public class UpdateSectionDto
    {
        public int? InstructorId { get; set; }
        public int? Capacity { get; set; }
        public string? ScheduleJson { get; set; }
        public int? ClassroomId { get; set; }
    }

    // ==================== CLASSROOM DTOs ====================
    
    public class ClassroomDto
    {
        public int Id { get; set; }
        public string Building { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? FeaturesJson { get; set; }
    }

    // ==================== COURSE APPLICATION DTOs ====================
    
    public class CreateCourseApplicationDto
    {
        public int CourseId { get; set; }
    }

    public class CourseApplicationDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public CourseDto? Course { get; set; }
        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string InstructorEmail { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class ApplicationListResponseDto
    {
        public List<CourseApplicationDto> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // ==================== STUDENT COURSE APPLICATION DTOs ====================
    
    public class CreateStudentCourseApplicationDto
    {
        public int CourseId { get; set; }
        public int SectionId { get; set; }
    }

    public class StudentCourseApplicationDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public CourseDto? Course { get; set; }
        public int SectionId { get; set; }
        public CourseSectionDto? Section { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class StudentApplicationListResponseDto
    {
        public List<StudentCourseApplicationDto> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

}
