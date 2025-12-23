using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

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

    // ==================== CSP ALGORITHM CLASSES ====================

    /// <summary>
    /// CSP (Constraint Satisfaction Problem) için zaman dilimi
    /// </summary>
    public class TimeSlot
    {
        public int DayOfWeek { get; set; } // 1=Pazartesi, 5=Cuma
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    /// <summary>
    /// CSP için atama (Section -> TimeSlot + Classroom)
    /// </summary>
    public class ScheduleAssignment
    {
        public int SectionId { get; set; }
        public int ClassroomId { get; set; }
        public TimeSlot TimeSlot { get; set; } = new();
    }

    /// <summary>
    /// CSP için değişken (Section)
    /// </summary>
    public class ScheduleVariable
    {
        public CourseSection Section { get; set; } = null!;
        public List<TimeSlot> Domain { get; set; } = new(); // Olası zaman dilimleri
        public List<int> SuitableClassrooms { get; set; } = new(); // Uygun sınıflar
        public int Priority { get; set; } // Öncelik (yüksek kapasite, zorunlu ders = yüksek öncelik)
    }

    // ==================== SERVICE ====================

    public class SchedulingService : ISchedulingService
    {
        private readonly CampusDbContext _context;
        
        // Zaman dilimleri: Pazartesi-Cuma, 09:00-17:00 (2 saatlik bloklar)
        private static readonly TimeSpan[] TimeSlots = new[]
        {
            new TimeSpan(9, 0, 0),   // 09:00-11:00
            new TimeSpan(11, 0, 0), // 11:00-13:00
            new TimeSpan(13, 0, 0), // 13:00-15:00
            new TimeSpan(15, 0, 0)  // 15:00-17:00
        };

        private static readonly int[] WeekDays = { 1, 2, 3, 4, 5 }; // Pazartesi-Cuma
        private static readonly string[] DayNames = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };

        public SchedulingService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleGenerationResultDto> GenerateScheduleAsync(GenerateScheduleDto dto)
        {
            var result = new ScheduleGenerationResultDto();

            // 1. Bölümleri (sections) al
            var sectionsQuery = _context.CourseSections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Where(s => !s.IsDeleted);

            if (dto.SectionIds != null && dto.SectionIds.Any())
                sectionsQuery = sectionsQuery.Where(s => dto.SectionIds.Contains(s.Id));

            var sections = await sectionsQuery.ToListAsync();

            if (!sections.Any())
            {
                result.Success = false;
                result.Message = "No sections to schedule";
                return result;
            }

            // 2. Sınıfları (classrooms) al
            var classrooms = await _context.Classrooms
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.Capacity)
                .ToListAsync();

            if (!classrooms.Any())
            {
                result.Success = false;
                result.Message = "No available classrooms";
                return result;
            }

            // 3. Öğrenci kayıtlarını (enrollments) al - öğrenci çakışması kontrolü için
            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                .Where(e => e.Status == "Active" && 
                           sections.Select(s => s.Id).Contains(e.SectionId))
                .ToListAsync();

            // 4. Mevcut programları temizle
            var existingSchedules = await _context.Schedules
                .Where(s => s.Semester == dto.Semester && s.Year == dto.Year)
                .ToListAsync();
            _context.Schedules.RemoveRange(existingSchedules);
            await _context.SaveChangesAsync();

            // 5. CSP Değişkenlerini oluştur (her section bir değişken)
            var variables = new List<ScheduleVariable>();
            foreach (var section in sections)
            {
                var variable = new ScheduleVariable
                {
                    Section = section,
                    Domain = GenerateTimeSlotDomain(), // Tüm olası zaman dilimleri
                    SuitableClassrooms = classrooms
                        .Where(c => c.Capacity >= section.Capacity && 
                                   MatchesClassroomFeatures(c, section))
                        .Select(c => c.Id)
                        .ToList(),
                    Priority = CalculatePriority(section)
                };
                variables.Add(variable);
            }

            // 6. Önceliğe göre sırala (yüksek öncelik önce)
            variables = variables.OrderByDescending(v => v.Priority).ToList();

            // 7. Backtracking ile CSP çöz
            var assignments = new List<ScheduleAssignment>();
            var conflicts = new List<string>();
            
            var success = await SolveCSP(variables, classrooms, enrollments, assignments, conflicts, dto.Semester, dto.Year);

            if (success)
            {
                // 8. Başarılı atamaları veritabanına kaydet
                foreach (var assignment in assignments)
                {
                    var schedule = new Schedule
                    {
                        SectionId = assignment.SectionId,
                        DayOfWeek = assignment.TimeSlot.DayOfWeek,
                        StartTime = assignment.TimeSlot.StartTime,
                        EndTime = assignment.TimeSlot.EndTime,
                        ClassroomId = assignment.ClassroomId,
                        Semester = dto.Semester,
                        Year = dto.Year,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Schedules.Add(schedule);
                }

                await _context.SaveChangesAsync();

                // 9. Sonuçları yükle
                var createdSchedules = await _context.Schedules
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Instructor)
                    .Include(s => s.Classroom)
                    .Where(s => s.Semester == dto.Semester && s.Year == dto.Year)
                    .ToListAsync();

                result.Success = true;
                result.ScheduledCount = assignments.Count;
                result.FailedCount = sections.Count - assignments.Count;
                result.Message = $"Successfully scheduled {assignments.Count} of {sections.Count} sections";
                result.Schedules = createdSchedules.Select(MapToScheduleDto).ToList();
                result.Conflicts = conflicts;
            }
            else
            {
                result.Success = false;
                result.ScheduledCount = assignments.Count;
                result.FailedCount = sections.Count - assignments.Count;
                result.Message = $"Could not schedule all sections. Scheduled: {assignments.Count}/{sections.Count}";
                result.Schedules = new List<ScheduleDto>();
                result.Conflicts = conflicts;
            }

            return result;
        }

        /// <summary>
        /// Tüm olası zaman dilimlerini oluştur (Pazartesi-Cuma, 09:00-17:00)
        /// </summary>
        private List<TimeSlot> GenerateTimeSlotDomain()
        {
            var domain = new List<TimeSlot>();
            foreach (var day in WeekDays)
            {
                foreach (var startTime in TimeSlots)
                {
                    domain.Add(new TimeSlot
                    {
                        DayOfWeek = day,
                        StartTime = startTime,
                        EndTime = startTime.Add(TimeSpan.FromHours(2))
                    });
                }
            }
            return domain;
        }

        /// <summary>
        /// Sınıf özelliklerinin ders gereksinimleriyle eşleşip eşleşmediğini kontrol et
        /// </summary>
        private bool MatchesClassroomFeatures(Classroom classroom, CourseSection section)
        {
            // Eğer sınıfın özellikleri yoksa, her ders için uygundur
            if (string.IsNullOrEmpty(classroom.FeaturesJson))
                return true;

            // Eğer dersin özel gereksinimleri yoksa, her sınıf uygundur
            // (Bu kısım gelecekte Course entity'sine RequirementsJson eklenerek genişletilebilir)
            // Şimdilik tüm sınıflar uygun kabul ediliyor
            return true;
        }

        /// <summary>
        /// Section önceliğini hesapla (yüksek kapasite, zorunlu ders = yüksek öncelik)
        /// </summary>
        private int CalculatePriority(CourseSection section)
        {
            int priority = 0;
            
            // Kapasiteye göre öncelik (büyük sınıflar önce)
            priority += section.Capacity * 10;
            
            // Zorunlu derslere öncelik
            if (section.Course?.Type == CourseType.Required)
                priority += 1000;
            else if (section.Course?.Type == CourseType.Elective)
                priority += 500;
            
            // Çok kayıtlı derslere öncelik
            priority += section.EnrolledCount * 5;
            
            return priority;
        }

        /// <summary>
        /// Backtracking ile CSP çözümü
        /// </summary>
        private async Task<bool> SolveCSP(
            List<ScheduleVariable> variables,
            List<Classroom> classrooms,
            List<Enrollment> enrollments,
            List<ScheduleAssignment> assignments,
            List<string> conflicts,
            string semester,
            int year)
        {
            // Backtracking başlat
            return await Backtrack(variables, 0, classrooms, enrollments, assignments, conflicts, semester, year);
        }

        /// <summary>
        /// Backtracking algoritması (recursive)
        /// </summary>
        private async Task<bool> Backtrack(
            List<ScheduleVariable> variables,
            int variableIndex,
            List<Classroom> classrooms,
            List<Enrollment> enrollments,
            List<ScheduleAssignment> assignments,
            List<string> conflicts,
            string semester,
            int year)
        {
            // Tüm değişkenler atandıysa başarılı
            if (variableIndex >= variables.Count)
                return true;

            var variable = variables[variableIndex];
            var section = variable.Section;

            // Domain'deki her zaman dilimi için dene (soft constraints'e göre sıralı)
            // Mevcut atamalara göre değerlendir (öğrenci boşlukları için)
            var orderedTimeSlots = variable.Domain
                .OrderBy(ts => EvaluateTimeSlot(ts, section, assignments))
                .ToList();
            
            foreach (var timeSlot in orderedTimeSlots)
            {
                // Uygun sınıfları dene
                foreach (var classroomId in variable.SuitableClassrooms)
                {
                    var assignment = new ScheduleAssignment
                    {
                        SectionId = section.Id,
                        ClassroomId = classroomId,
                        TimeSlot = timeSlot
                    };

                    // Hard Constraints kontrolü
                    if (IsValidAssignment(assignment, assignments, variables, enrollments, section))
                    {
                        // Atamayı ekle
                        assignments.Add(assignment);

                        // Recursive olarak devam et
                        var success = await Backtrack(variables, variableIndex + 1, classrooms, enrollments, assignments, conflicts, semester, year);
                        
                        if (success)
                            return true;

                        // Backtrack: Atamayı geri al
                        assignments.Remove(assignment);
                    }
                }
            }

            // Bu değişken için uygun atama bulunamadı
            conflicts.Add($"{section.Course?.Code} - {section.SectionNumber}: Uygun zaman dilimi bulunamadı");
            return false;
        }

        /// <summary>
        /// Hard Constraints kontrolü
        /// </summary>
        private bool IsValidAssignment(
            ScheduleAssignment assignment,
            List<ScheduleAssignment> existingAssignments,
            List<ScheduleVariable> variables,
            List<Enrollment> enrollments,
            CourseSection section)
        {
            // 1. Eğitmen çifte rezervasyon kontrolü
            var instructorConflict = existingAssignments.Any(a =>
            {
                // Variables listesinden section'ı bul
                var existingVariable = variables.FirstOrDefault(v => v.Section.Id == a.SectionId);
                if (existingVariable == null) return false;
                
                return existingVariable.Section.InstructorId == section.InstructorId &&
                       a.TimeSlot.DayOfWeek == assignment.TimeSlot.DayOfWeek &&
                       IsTimeOverlapping(a.TimeSlot, assignment.TimeSlot);
            });

            if (instructorConflict)
                return false;

            // 2. Sınıf çifte rezervasyon kontrolü
            var classroomConflict = existingAssignments.Any(a =>
                a.ClassroomId == assignment.ClassroomId &&
                a.TimeSlot.DayOfWeek == assignment.TimeSlot.DayOfWeek &&
                IsTimeOverlapping(a.TimeSlot, assignment.TimeSlot));

            if (classroomConflict)
                return false;

            // 3. Öğrenci program çakışması kontrolü (enrollments'e göre)
            var sectionEnrollments = enrollments.Where(e => e.SectionId == section.Id).ToList();
            foreach (var enrollment in sectionEnrollments)
            {
                var studentId = enrollment.StudentId;
                
                // Bu öğrencinin diğer aktif derslerini kontrol et
                var otherEnrollments = enrollments
                    .Where(e => e.StudentId == studentId && 
                               e.SectionId != section.Id &&
                               e.Status == "Active")
                    .ToList();

                foreach (var otherEnrollment in otherEnrollments)
                {
                    var otherAssignment = existingAssignments
                        .FirstOrDefault(a => a.SectionId == otherEnrollment.SectionId);

                    if (otherAssignment != null)
                    {
                        // Aynı gün ve çakışan saatlerde başka bir dersi varsa çakışma
                        if (otherAssignment.TimeSlot.DayOfWeek == assignment.TimeSlot.DayOfWeek &&
                            IsTimeOverlapping(otherAssignment.TimeSlot, assignment.TimeSlot))
                        {
                            return false;
                        }
                    }
                }
            }

            // 4. Sınıf kapasitesi kontrolü (zaten SuitableClassrooms'da filtrelenmiş, ama tekrar kontrol)
            // Bu kontrol zaten variable oluşturulurken yapıldı, burada sadece güvenlik kontrolü

            // 5. Sınıf özellikleri kontrolü (zaten MatchesClassroomFeatures'da kontrol edilmiş)
            // Burada ek kontrol gerekirse yapılabilir

            return true;
        }

        /// <summary>
        /// İki zaman diliminin çakışıp çakışmadığını kontrol et
        /// </summary>
        private bool IsTimeOverlapping(TimeSlot slot1, TimeSlot slot2)
        {
            return (slot1.StartTime < slot2.EndTime && slot1.EndTime > slot2.StartTime);
        }

        /// <summary>
        /// TimeSlot değerini değerlendir (soft constraints için)
        /// Düşük değer = daha iyi (sabah saatleri, zorunlu dersler için)
        /// </summary>
        private int EvaluateTimeSlot(TimeSlot timeSlot, CourseSection section, List<ScheduleAssignment> existingAssignments = null)
        {
            int score = 0;

            // Soft Constraint 1: Zorunlu dersler için sabah saatlerini tercih et
            if (section.Course?.Type == CourseType.Required)
            {
                // Sabah saatleri (09:00, 11:00) daha düşük skor (tercih edilir)
                if (timeSlot.StartTime.Hours == 9)
                    score -= 200; // En çok tercih edilen
                else if (timeSlot.StartTime.Hours == 11)
                    score -= 100;
                else if (timeSlot.StartTime.Hours == 13)
                    score += 50;
                else if (timeSlot.StartTime.Hours == 15)
                    score += 100; // En az tercih edilen
            }
            else
            {
                // Seçmeli dersler için daha esnek
                if (timeSlot.StartTime.Hours == 9)
                    score -= 50;
                else if (timeSlot.StartTime.Hours == 11)
                    score -= 25;
            }

            // Soft Constraint 2: Hafta içi eşit dağılım
            // Salı ve Perşembe en tercih edilen (score = 0)
            // Çarşamba orta (score = 5)
            // Pazartesi ve Cuma daha az tercih edilir (score = 10)
            if (timeSlot.DayOfWeek == 1 || timeSlot.DayOfWeek == 5) // Pazartesi, Cuma
                score += 10;
            else if (timeSlot.DayOfWeek == 3) // Çarşamba
                score += 5;
            // Salı (2) ve Perşembe (4) score = 0 (en tercih edilen)

            // Soft Constraint 3: Öğrenci programlarındaki boşlukları minimize et
            if (existingAssignments != null)
            {
                score += CalculateStudentGapPenalty(timeSlot, section, existingAssignments);
            }

            // Soft Constraint 4: Eğitmen tercihleri (gelecekte eklenebilir)
            // InstructorPreferences tablosu eklenerek genişletilebilir
            // Şimdilik her zaman aynı skor

            return score;
        }

        /// <summary>
        /// Öğrenci programlarındaki boşlukları hesapla (penalty)
        /// </summary>
        private int CalculateStudentGapPenalty(TimeSlot timeSlot, CourseSection section, List<ScheduleAssignment> existingAssignments)
        {
            // Bu section'a kayıtlı öğrencileri bul
            // (Bu bilgi enrollments'ten alınabilir, ama şimdilik basit bir heuristik kullanıyoruz)
            
            // Eğer bu günde ve saatte başka dersler varsa, öğrenciler için daha iyi (boşluk azalır)
            // Eğer yalnız kalırsa, boşluk artar (penalty)
            
            var sameDayAssignments = existingAssignments
                .Where(a => a.TimeSlot.DayOfWeek == timeSlot.DayOfWeek)
                .ToList();

            if (sameDayAssignments.Count == 0)
            {
                // Bu günde hiç ders yok, boşluk oluşur (penalty)
                return 20;
            }

            // Komşu saatlerde dersler varsa, boşluk azalır (bonus)
            var hasAdjacentSlot = sameDayAssignments.Any(a =>
            {
                var timeDiff = Math.Abs((a.TimeSlot.StartTime - timeSlot.StartTime).TotalHours);
                return timeDiff == 2; // 2 saat arayla (komşu slot)
            });

            if (hasAdjacentSlot)
            {
                return -10; // Bonus: komşu slot'ta ders var, boşluk yok
            }

            return 0; // Nötr
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
            // Check if user is instructor (faculty)
            var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.UserId == userId);
            if (faculty != null)
            {
                // For faculty, InstructorId in CourseSection is the User.Id
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
                // Get enrolled sections - Enrollment.StudentId references Student.Id, not User.Id
                var enrolledSectionIds = await _context.Enrollments
                    .Where(e => e.StudentId == student.Id && (e.Status == "Active" || e.Status == "enrolled"))
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

            // For Admin users, return all schedules for the semester
            var adminSchedules = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                .Include(s => s.Classroom)
                .Where(s => s.Semester == semester && s.Year == year && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return adminSchedules.Select(MapToScheduleDto).ToList();
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
