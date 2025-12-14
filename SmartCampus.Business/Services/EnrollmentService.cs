using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;

namespace SmartCampus.Business.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentDto> EnrollAsync(int studentId, int sectionId);
        Task<bool> DropCourseAsync(int enrollmentId, int studentId);
        Task<List<MyCoursesDto>> GetMyCoursesAsync(int studentId);
        Task<List<StudentEnrollmentDto>> GetSectionStudentsAsync(int sectionId);
        Task<MyGradesDto> GetMyGradesAsync(int studentId);
        Task<GradeResultDto> EnterGradeAsync(int instructorId, GradeInputDto dto);
        Task<TranscriptDto> GetTranscriptAsync(int studentId);
    }

    public class EnrollmentService : IEnrollmentService
    {
        private readonly CampusDbContext _context;
        private readonly IGradeCalculationService _gradeService;

        public EnrollmentService(CampusDbContext context, IGradeCalculationService gradeService)
        {
            _context = context;
            _gradeService = gradeService;
        }

        public async Task<EnrollmentDto> EnrollAsync(int studentId, int sectionId)
        {
            var section = await _context.CourseSections
                .Include(s => s.Course)
                    .ThenInclude(c => c!.Prerequisites)
                .FirstOrDefaultAsync(s => s.Id == sectionId && !s.IsDeleted);

            if (section == null)
                throw new Exception("Section not found");

            // 1. Check if already enrolled
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SectionId == sectionId);
            if (existingEnrollment != null)
                throw new InvalidOperationException("Already enrolled in this section");

            // 2. Check prerequisites
            await CheckPrerequisitesAsync(section.Course!.Id, studentId);

            // 3. Check schedule conflict
            await CheckScheduleConflictAsync(studentId, section);

            // 4. Check capacity (atomic update)
            var affected = await _context.CourseSections
                .Where(s => s.Id == sectionId && s.EnrolledCount < s.Capacity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrolledCount, x => x.EnrolledCount + 1));

            if (affected == 0)
                throw new InvalidOperationException("Section is full");

            // 5. Create enrollment
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                SectionId = sectionId,
                Status = "enrolled",
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return await MapToEnrollmentDtoAsync(enrollment);
        }

        private async Task CheckPrerequisitesAsync(int courseId, int studentId)
        {
            var prerequisites = await _context.CoursePrerequisites
                .Include(cp => cp.PrerequisiteCourse)
                .Where(cp => cp.CourseId == courseId)
                .ToListAsync();

            foreach (var prereq in prerequisites)
            {
                // Check if student completed this prerequisite
                var completed = await _context.Enrollments
                    .Include(e => e.Section)
                    .AnyAsync(e => e.StudentId == studentId &&
                                   e.Section!.CourseId == prereq.PrerequisiteCourseId &&
                                   e.Status == "completed" &&
                                   e.LetterGrade != "F");

                if (!completed)
                {
                    throw new InvalidOperationException($"Prerequisite not met: {prereq.PrerequisiteCourse?.Code}");
                }

                // Recursive check for nested prerequisites
                await CheckPrerequisitesAsync(prereq.PrerequisiteCourseId, studentId);
            }
        }

        private async Task CheckScheduleConflictAsync(int studentId, CourseSection newSection)
        {
            if (string.IsNullOrEmpty(newSection.ScheduleJson)) return;

            var currentEnrollments = await _context.Enrollments
                .Include(e => e.Section)
                .Where(e => e.StudentId == studentId &&
                           e.Status == "enrolled" &&
                           e.Section!.Semester == newSection.Semester &&
                           e.Section.Year == newSection.Year)
                .ToListAsync();

            var newSchedule = ParseSchedule(newSection.ScheduleJson);

            foreach (var enrollment in currentEnrollments)
            {
                if (string.IsNullOrEmpty(enrollment.Section?.ScheduleJson)) continue;

                var existingSchedule = ParseSchedule(enrollment.Section.ScheduleJson);

                if (HasTimeOverlap(existingSchedule, newSchedule))
                {
                    throw new InvalidOperationException($"Schedule conflict with {enrollment.Section.Course?.Code}");
                }
            }
        }

        private Dictionary<string, List<string>> ParseSchedule(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) 
                       ?? new Dictionary<string, List<string>>();
            }
            catch
            {
                return new Dictionary<string, List<string>>();
            }
        }

        private bool HasTimeOverlap(Dictionary<string, List<string>> schedule1, Dictionary<string, List<string>> schedule2)
        {
            foreach (var day in schedule1.Keys)
            {
                if (!schedule2.ContainsKey(day)) continue;

                foreach (var time1 in schedule1[day])
                {
                    foreach (var time2 in schedule2[day])
                    {
                        if (TimesOverlap(time1, time2)) return true;
                    }
                }
            }
            return false;
        }

        private bool TimesOverlap(string time1, string time2)
        {
            // Format: "09:00-10:30"
            var parts1 = time1.Split('-');
            var parts2 = time2.Split('-');

            if (parts1.Length != 2 || parts2.Length != 2) return false;

            var start1 = TimeSpan.Parse(parts1[0]);
            var end1 = TimeSpan.Parse(parts1[1]);
            var start2 = TimeSpan.Parse(parts2[0]);
            var end2 = TimeSpan.Parse(parts2[1]);

            return start1 < end2 && start2 < end1;
        }

        public async Task<bool> DropCourseAsync(int enrollmentId, int studentId)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Section)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == studentId);

            if (enrollment == null) return false;

            // Check 4-week rule
            var weeksSinceEnrollment = (DateTime.UtcNow - enrollment.EnrollmentDate).TotalDays / 7;
            if (weeksSinceEnrollment > 4)
            {
                throw new InvalidOperationException("Cannot drop course after 4 weeks");
            }

            enrollment.Status = "dropped";
            enrollment.UpdatedAt = DateTime.UtcNow;

            // Decrease enrolled count
            await _context.CourseSections
                .Where(s => s.Id == enrollment.SectionId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrolledCount, x => x.EnrolledCount - 1));

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MyCoursesDto>> GetMyCoursesAsync(int studentId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Instructor)
                .Where(e => e.StudentId == studentId && e.Status == "enrolled")
                .ToListAsync();

            var result = new List<MyCoursesDto>();
            foreach (var e in enrollments)
            {
                var attendance = await CalculateAttendancePercentageAsync(studentId, e.SectionId);
                result.Add(new MyCoursesDto
                {
                    Id = e.Id,
                    Section = MapToSectionDto(e.Section!),
                    Status = e.Status,
                    EnrollmentDate = e.EnrollmentDate,
                    AttendancePercentage = attendance
                });
            }
            return result;
        }

        public async Task<List<StudentEnrollmentDto>> GetSectionStudentsAsync(int sectionId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.SectionId == sectionId && e.Status != "dropped")
                .ToListAsync();

            var studentIdsList = new List<int>();
            foreach (var en in enrollments)
            {
                studentIdsList.Add(en.StudentId);
            }
            
            var studentList = await _context.Students
                .Where(s => studentIdsList.Contains(s.UserId))
                .ToListAsync();
            var students = studentList.ToDictionary(s => s.UserId);

            var result = new List<StudentEnrollmentDto>();
            foreach (var e in enrollments)
            {
                result.Add(new StudentEnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FirstName + " " + e.Student?.LastName,
                    StudentNumber = students.ContainsKey(e.StudentId) ? students[e.StudentId].StudentNumber : "",
                    MidtermGrade = e.MidtermGrade,
                    FinalGrade = e.FinalGrade,
                    HomeworkGrade = e.HomeworkGrade,
                    LetterGrade = e.LetterGrade,
                    GradePoint = e.GradePoint
                });
            }
            return result;
        }

        public async Task<MyGradesDto> GetMyGradesAsync(int studentId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Where(e => e.StudentId == studentId && e.Status != "dropped")
                .ToListAsync();

            var grades = enrollments.Select(e => new GradeDto
            {
                EnrollmentId = e.Id,
                CourseCode = e.Section?.Course?.Code ?? "",
                CourseName = e.Section?.Course?.Name ?? "",
                Credits = e.Section?.Course?.Credits ?? 0,
                Semester = e.Section?.Semester ?? "",
                Year = e.Section?.Year ?? 0,
                MidtermGrade = e.MidtermGrade,
                FinalGrade = e.FinalGrade,
                HomeworkGrade = e.HomeworkGrade,
                LetterGrade = e.LetterGrade,
                GradePoint = e.GradePoint
            }).ToList();

            var completedWithGrades = enrollments.Where(e => e.LetterGrade != null && e.Status == "completed").ToList();
            
            decimal? gpa = null;
            decimal? cgpa = null;

            if (completedWithGrades.Any())
            {
                var totalCredits = completedWithGrades.Sum(e => e.Section?.Course?.Credits ?? 0);
                var totalPoints = completedWithGrades.Sum(e => (e.GradePoint ?? 0) * (e.Section?.Course?.Credits ?? 0));
                cgpa = totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;
                gpa = cgpa; // Simplified - could be semester-specific
            }

            return new MyGradesDto { Data = grades, Gpa = gpa, Cgpa = cgpa };
        }

        public async Task<GradeResultDto> EnterGradeAsync(int instructorId, GradeInputDto dto)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Section)
                .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId);

            if (enrollment == null)
                throw new Exception("Enrollment not found");

            if (enrollment.Section?.InstructorId != instructorId)
                throw new UnauthorizedAccessException("Not authorized to grade this course");

            if (dto.MidtermGrade.HasValue) enrollment.MidtermGrade = dto.MidtermGrade;
            if (dto.FinalGrade.HasValue) enrollment.FinalGrade = dto.FinalGrade;
            if (dto.HomeworkGrade.HasValue) enrollment.HomeworkGrade = dto.HomeworkGrade;

            // Calculate letter grade if final is entered
            if (enrollment.FinalGrade.HasValue)
            {
                enrollment.LetterGrade = _gradeService.CalculateLetterGrade(
                    enrollment.MidtermGrade, enrollment.FinalGrade, enrollment.HomeworkGrade);
                enrollment.GradePoint = _gradeService.CalculateGradePoint(enrollment.LetterGrade);
                enrollment.Status = "completed";
            }

            enrollment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new GradeResultDto
            {
                Id = enrollment.Id,
                LetterGrade = enrollment.LetterGrade,
                GradePoint = enrollment.GradePoint,
                Message = "Grade saved successfully"
            };
        }

        public async Task<TranscriptDto> GetTranscriptAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (student == null)
                throw new Exception("Student not found");

            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Where(e => e.StudentId == studentId && e.Status == "completed" && e.LetterGrade != null)
                .OrderBy(e => e.Section!.Year)
                .ThenBy(e => e.Section!.Semester)
                .ToListAsync();

            var semesters = enrollments
                .GroupBy(e => new { e.Section!.Semester, e.Section.Year })
                .Select(g => new SemesterGradesDto
                {
                    Semester = g.Key.Semester,
                    Year = g.Key.Year,
                    SemesterCredits = g.Sum(e => e.Section?.Course?.Credits ?? 0),
                    Gpa = CalculateSemesterGpa(g.ToList()),
                    Courses = g.Select(e => new GradeDto
                    {
                        EnrollmentId = e.Id,
                        CourseCode = e.Section?.Course?.Code ?? "",
                        CourseName = e.Section?.Course?.Name ?? "",
                        Credits = e.Section?.Course?.Credits ?? 0,
                        Semester = e.Section?.Semester ?? "",
                        Year = e.Section?.Year ?? 0,
                        LetterGrade = e.LetterGrade,
                        GradePoint = e.GradePoint
                    }).ToList()
                }).ToList();

            var totalCredits = enrollments.Sum(e => e.Section?.Course?.Credits ?? 0);
            var totalPoints = enrollments.Sum(e => (e.GradePoint ?? 0) * (e.Section?.Course?.Credits ?? 0));
            var cgpa = totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;

            return new TranscriptDto
            {
                StudentName = student.User?.FirstName + " " + student.User?.LastName,
                StudentNumber = student.StudentNumber,
                Department = student.Department?.Name ?? "",
                TotalCredits = totalCredits,
                Cgpa = cgpa,
                Semesters = semesters
            };
        }

        private decimal CalculateSemesterGpa(List<Enrollment> enrollments)
        {
            var totalCredits = enrollments.Sum(e => e.Section?.Course?.Credits ?? 0);
            var totalPoints = enrollments.Sum(e => (e.GradePoint ?? 0) * (e.Section?.Course?.Credits ?? 0));
            return totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;
        }

        private async Task<decimal?> CalculateAttendancePercentageAsync(int studentId, int sectionId)
        {
            var totalSessions = await _context.AttendanceSessions
                .CountAsync(s => s.SectionId == sectionId);

            if (totalSessions == 0) return null;

            var attended = await _context.AttendanceRecords
                .CountAsync(r => r.StudentId == studentId && r.Session!.SectionId == sectionId);

            return Math.Round((decimal)attended / totalSessions * 100, 1);
        }

        private async Task<EnrollmentDto> MapToEnrollmentDtoAsync(Enrollment enrollment)
        {
            var section = await _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id == enrollment.SectionId);

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                SectionId = enrollment.SectionId,
                Section = section == null ? null : MapToSectionDto(section),
                Status = enrollment.Status,
                EnrollmentDate = enrollment.EnrollmentDate
            };
        }

        private CourseSectionDto MapToSectionDto(CourseSection section)
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
                InstructorName = section.Instructor?.FirstName + " " + section.Instructor?.LastName,
                Capacity = section.Capacity,
                EnrolledCount = section.EnrolledCount,
                ScheduleJson = section.ScheduleJson
            };
        }
    }
}
