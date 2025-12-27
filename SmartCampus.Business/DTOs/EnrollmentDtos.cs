namespace SmartCampus.Business.DTOs
{
    // ==================== ENROLLMENT DTOs ====================
    
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int SectionId { get; set; }
        public CourseSectionDto? Section { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
        public decimal? AttendancePercentage { get; set; }
    }

    public class CreateEnrollmentDto
    {
        public int SectionId { get; set; }
    }

    public class MyCoursesDto
    {
        public int Id { get; set; }
        public CourseSectionDto Section { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public decimal? AttendancePercentage { get; set; }
        // Grade information for completed courses
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
    }

    public class StudentEnrollmentDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
    }

    // ==================== GRADE DTOs ====================
    
    public class GradeDto
    {
        public int EnrollmentId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string Semester { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
    }

    public class MyGradesDto
    {
        public List<GradeDto> Data { get; set; } = new();
        public decimal? Gpa { get; set; }
        public decimal? Cgpa { get; set; }
    }

    public class GradeInputDto
    {
        public int EnrollmentId { get; set; }
        public decimal? MidtermGrade { get; set; }
        public decimal? FinalGrade { get; set; }
        public decimal? HomeworkGrade { get; set; }
    }

    public class GradeResultDto
    {
        public int Id { get; set; }
        public string? LetterGrade { get; set; }
        public decimal? GradePoint { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TranscriptDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Cgpa { get; set; }
        public int TotalCredits { get; set; }
        public List<SemesterGradesDto> Semesters { get; set; } = new();
    }

    public class SemesterGradesDto
    {
        public string Semester { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Gpa { get; set; }
        public int SemesterCredits { get; set; }
        public List<GradeDto> Courses { get; set; } = new();
    }
}
