namespace SmartCampus.Business.DTOs
{
    // ==================== ATTENDANCE SESSION DTOs ====================
    
    public class AttendanceSessionDto
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string SectionNumber { get; set; } = string.Empty;
        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal GeofenceRadius { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public DateTime QrCodeExpiry { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AttendedCount { get; set; }
        public int TotalStudents { get; set; }
        public ClassroomDto? Classroom { get; set; }
        public CourseSectionDto? Section { get; set; }
    }

    public class CreateAttendanceSessionDto
    {
        public int SectionId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal? GeofenceRadius { get; set; } = 15.0m;
    }

    // ==================== CHECK-IN DTOs ====================
    
    public class CheckInRequestDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal Accuracy { get; set; }
        
        // Advanced spoofing detection fields
        public bool? IsMockLocation { get; set; } // From Android/iOS mock location API
        public DateTime? Timestamp { get; set; } // GPS timestamp for velocity check
        public decimal? Altitude { get; set; } // For additional validation
        public decimal? Speed { get; set; } // Device reported speed (m/s)
        public string? DeviceType { get; set; } // "mobile" or "desktop" - for accuracy threshold adjustment
    }

    public class CheckInResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public bool IsFlagged { get; set; }
        public string? FlagReason { get; set; }
    }

    // ==================== MY ATTENDANCE DTOs ====================
    
    public class MyAttendanceDto
    {
        public int CourseId { get; set; }
        public int SectionId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string SectionNumber { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int ExcusedAbsences { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string Status { get; set; } = string.Empty; // "Good", "Warning", "Critical"
    }

    // ==================== ATTENDANCE REPORT DTOs ====================
    
    public class AttendanceReportDto
    {
        public CourseSectionDto Section { get; set; } = null!;
        public List<StudentAttendanceDto> Students { get; set; } = new();
    }

    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int ExcusedAbsences { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ==================== EXCUSE REQUEST DTOs ====================
    
    public class ExcuseRequestDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public AttendanceSessionDto? Session { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? DocumentUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ReviewedBy { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExcuseRequestDto
    {
        public int SessionId { get; set; }
        public string Reason { get; set; } = string.Empty;
        // Document will be handled via IFormFile in controller
    }

    public class ReviewExcuseDto
    {
        public string? Notes { get; set; }
    }
}
