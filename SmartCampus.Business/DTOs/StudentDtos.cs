namespace SmartCampus.Business.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public decimal CGPA { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateStudentStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class StudentListResponseDto
    {
        public List<StudentDto> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}

