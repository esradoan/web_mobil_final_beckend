using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace SmartCampus.Business.Services
{
    public interface ISchedulingService
    {
        Task<ScheduleGenerationResultDto> GenerateScheduleAsync(GenerateScheduleDto dto);
        Task<List<ScheduleDto>> GetScheduleAsync(string semester, int year);
        Task<ScheduleDto?> GetScheduleByIdAsync(int scheduleId);
        Task<List<ScheduleDto>> GetMyScheduleAsync(int userId, string semester, int year);
        Task<string> ExportToICalAsync(int userId, string semester, int year);
    }

    // ==================== DTOs ====================

    public class GenerateScheduleDto
    {
        public string Semester { get; set; } = "fall";
        public int Year { get; set; }
        public List<int>? SectionIds { get; set; }
    }

    public class ScheduleGenerationResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int ScheduledCount { get; set; }
        public int FailedCount { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
    }

    public class ScheduleDto
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string ClassroomName { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
    }

    // ==================== SERVICE ====================

    public class SchedulingService : ISchedulingService
    {
        private readonly CampusDbContext _context;
        
        // Time slots: 09:00-17:00 (4 x 2-hour slots)
        private static readonly TimeSpan[] TimeSlots = new[]
        {
            new TimeSpan(9, 0, 0),
            new TimeSpan(11, 0, 0),
            new TimeSpan(13, 0, 0),
            new TimeSpan(15, 0, 0)
        };

        private static readonly string[] DayNames = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };

        public SchedulingService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleGenerationResultDto> GenerateScheduleAsync(GenerateScheduleDto dto)
        {
            var result = new ScheduleGenerationResultDto();

            // Get sections to schedule
            var sectionsQuery = _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Where(s => !s.IsDeleted);

            if (dto.SectionIds != null && dto.SectionIds.Any())
                sectionsQuery = sectionsQuery.Where(s => dto.SectionIds.Contains(s.Id));

            var sections = await sectionsQuery.ToListAsync();

            // Get available classrooms
            var classrooms = await _context.Classrooms
                .OrderByDescending(c => c.Capacity)
                .ToListAsync();

            if (!classrooms.Any())
            {
                result.Success = false;
                result.Message = "No available classrooms";
                return result;
            }

            // Clear existing schedules for this semester/year
            var existingSchedules = await _context.Schedules
                .Where(s => s.Semester == dto.Semester && s.Year == dto.Year)
                .ToListAsync();
            _context.Schedules.RemoveRange(existingSchedules);

            // Track assignments: (ClassroomId, DayOfWeek, TimeSlot) -> SectionId
            var classroomAssignments = new Dictionary<(int, int, TimeSpan), int>();
            // Track instructor assignments: (InstructorId, DayOfWeek, TimeSlot) -> true
            var instructorAssignments = new Dictionary<(int, int, TimeSpan), bool>();

            var scheduledSections = new List<Schedule>();
            var conflicts = new List<string>();

            // Sort sections by capacity (larger classes first - harder to place)
            var sortedSections = sections.OrderByDescending(s => s.Capacity).ToList();

            foreach (var section in sortedSections)
            {
                bool scheduled = false;

                // Try each day (Monday-Friday: 1-5)
                for (int day = 1; day <= 5 && !scheduled; day++)
                {
                    // Try each time slot
                    foreach (var timeSlot in TimeSlots)
                    {
                        if (scheduled) break;

                        // Check instructor availability
                        var instructorKey = (section.InstructorId, day, timeSlot);
                        if (instructorAssignments.ContainsKey(instructorKey))
                            continue;

                        // Find suitable classroom
                        var suitableRoom = classrooms.FirstOrDefault(c =>
                            c.Capacity >= section.Capacity &&
                            !classroomAssignments.ContainsKey((c.Id, day, timeSlot)));

                        if (suitableRoom != null)
                        {
                            // Create schedule
                            var schedule = new Schedule
                            {
                                SectionId = section.Id,
                                DayOfWeek = day,
                                StartTime = timeSlot,
                                EndTime = timeSlot.Add(TimeSpan.FromHours(2)),
                                ClassroomId = suitableRoom.Id,
                                Semester = dto.Semester,
                                Year = dto.Year,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            scheduledSections.Add(schedule);
                            _context.Schedules.Add(schedule);

                            // Mark as assigned
                            classroomAssignments[(suitableRoom.Id, day, timeSlot)] = section.Id;
                            instructorAssignments[instructorKey] = true;

                            scheduled = true;
                        }
                    }
                }

                if (!scheduled)
                {
                    result.FailedCount++;
                    conflicts.Add($"{section.Course?.Code} - {section.SectionNumber}: No available slot");
                }
            }

            await _context.SaveChangesAsync();

            // Load related data for result
            var createdSchedules = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                .Include(s => s.Classroom)
                .Where(s => s.Semester == dto.Semester && s.Year == dto.Year)
                .ToListAsync();

            result.Success = true;
            result.ScheduledCount = scheduledSections.Count;
            result.Message = $"Successfully scheduled {scheduledSections.Count} sections";
            result.Schedules = createdSchedules.Select(MapToScheduleDto).ToList();
            result.Conflicts = conflicts;

            return result;
        }

        public async Task<List<ScheduleDto>> GetScheduleAsync(string semester, int year)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                .Include(s => s.Classroom)
                .Where(s => s.Semester == semester && s.Year == year && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return schedules.Select(MapToScheduleDto).ToList();
        }

        public async Task<ScheduleDto?> GetScheduleByIdAsync(int scheduleId)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            return schedule == null ? null : MapToScheduleDto(schedule);
        }

        public async Task<List<ScheduleDto>> GetMyScheduleAsync(int userId, string semester, int year)
        {
            // Check if user is instructor
            var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == userId);
            if (faculty != null)
            {
                var instructorSchedules = await _context.Schedules
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Instructor)
                    .Include(s => s.Classroom)
                    .Where(s => s.Semester == semester && s.Year == year && s.IsActive &&
                                s.Section!.InstructorId == userId)
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .ToListAsync();

                return instructorSchedules.Select(MapToScheduleDto).ToList();
            }

            // Check if user is student
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student != null)
            {
                // Get enrolled sections
                var enrolledSectionIds = await _context.Enrollments
                    .Where(e => e.StudentId == userId && e.Status == "Active")
                    .Select(e => e.SectionId)
                    .ToListAsync();

                var studentSchedules = await _context.Schedules
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Instructor)
                    .Include(s => s.Classroom)
                    .Where(s => s.Semester == semester && s.Year == year && s.IsActive &&
                                enrolledSectionIds.Contains(s.SectionId))
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .ToListAsync();

                return studentSchedules.Select(MapToScheduleDto).ToList();
            }

            return new List<ScheduleDto>();
        }

        public async Task<string> ExportToICalAsync(int userId, string semester, int year)
        {
            var schedules = await GetMyScheduleAsync(userId, semester, year);

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//SmartCampus//Schedule//TR");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");

            // Calculate semester start date
            var semesterStart = semester == "fall" 
                ? new DateTime(year, 9, 9) 
                : semester == "spring" 
                    ? new DateTime(year, 2, 10) 
                    : new DateTime(year, 6, 17);

            // Adjust to correct day of week
            foreach (var schedule in schedules)
            {
                var eventDate = semesterStart;
                while ((int)eventDate.DayOfWeek != schedule.DayOfWeek)
                    eventDate = eventDate.AddDays(1);

                var startDateTime = eventDate.Add(schedule.StartTime);
                var endDateTime = eventDate.Add(schedule.EndTime);

                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"DTSTART:{startDateTime:yyyyMMddTHHmmss}");
                sb.AppendLine($"DTEND:{endDateTime:yyyyMMddTHHmmss}");
                sb.AppendLine($"RRULE:FREQ=WEEKLY;COUNT=14");
                sb.AppendLine($"SUMMARY:{schedule.CourseCode} - {schedule.CourseName}");
                sb.AppendLine($"LOCATION:{schedule.Building} {schedule.ClassroomName}");
                sb.AppendLine($"DESCRIPTION:Instructor: {schedule.InstructorName}");
                sb.AppendLine($"UID:{Guid.NewGuid()}@smartcampus");
                sb.AppendLine("END:VEVENT");
            }

            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }

        private ScheduleDto MapToScheduleDto(Schedule s)
        {
            return new ScheduleDto
            {
                Id = s.Id,
                SectionId = s.SectionId,
                CourseCode = s.Section?.Course?.Code ?? "",
                CourseName = s.Section?.Course?.Name ?? "",
                InstructorName = s.Section?.Instructor != null 
                    ? $"{s.Section.Instructor.FirstName} {s.Section.Instructor.LastName}" 
                    : "",
                DayOfWeek = s.DayOfWeek,
                DayName = DayNames[s.DayOfWeek],
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                ClassroomName = s.Classroom?.RoomNumber ?? "",
                Building = s.Classroom?.Building ?? ""
            };
        }
    }
}
