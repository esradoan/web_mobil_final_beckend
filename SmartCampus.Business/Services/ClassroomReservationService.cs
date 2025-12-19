using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IClassroomReservationService
    {
        // CRUD
        Task<ClassroomReservationDto> CreateReservationAsync(int userId, CreateClassroomReservationDto dto);
        Task<List<ClassroomReservationDto>> GetReservationsAsync(int? classroomId = null, DateTime? date = null, string? status = null);
        Task<ClassroomReservationDto?> GetReservationByIdAsync(int id);
        Task<List<ClassroomReservationDto>> GetMyReservationsAsync(int userId);
        Task<bool> CancelReservationAsync(int userId, int reservationId);
        
        // Approval Workflow
        Task<ClassroomReservationDto?> ApproveReservationAsync(int adminUserId, int reservationId, string? notes = null);
        Task<ClassroomReservationDto?> RejectReservationAsync(int adminUserId, int reservationId, string? notes = null);
        Task<List<ClassroomReservationDto>> GetPendingReservationsAsync();
        
        // Classroom availability
        Task<List<ClassroomAvailabilityDto>> GetClassroomAvailabilityAsync(int classroomId, DateTime date);
        Task<List<AvailableClassroomDto>> GetAvailableClassroomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime);
    }

    // ==================== DTOs ====================

    public class ClassroomReservationDto
    {
        public int Id { get; set; }
        public int ClassroomId { get; set; }
        public string ClassroomName { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ApprovedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateClassroomReservationDto
    {
        public int ClassroomId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    public class ClassroomAvailabilityDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public string? ReservedBy { get; set; }
        public string? Purpose { get; set; }
    }

    public class AvailableClassroomDto
    {
        public int Id { get; set; }
        public string Building { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }

    // ==================== SERVICE ====================

    public class ClassroomReservationService : IClassroomReservationService
    {
        private readonly CampusDbContext _context;

        public ClassroomReservationService(CampusDbContext context)
        {
            _context = context;
        }

        // ==================== CRUD ====================

        public async Task<ClassroomReservationDto> CreateReservationAsync(int userId, CreateClassroomReservationDto dto)
        {
            // Validate classroom exists
            var classroom = await _context.Classrooms.FindAsync(dto.ClassroomId);
            if (classroom == null)
                throw new Exception("Classroom not found");

            // Validate time range
            if (dto.StartTime >= dto.EndTime)
                throw new Exception("End time must be after start time");

            if (dto.Date.Date < DateTime.UtcNow.Date)
                throw new Exception("Cannot reserve for past dates");

            // Check for conflicts (approved or pending reservations)
            var hasConflict = await _context.ClassroomReservations
                .AnyAsync(r => r.ClassroomId == dto.ClassroomId &&
                              r.Date.Date == dto.Date.Date &&
                              r.Status != "rejected" && r.Status != "cancelled" &&
                              ((dto.StartTime >= r.StartTime && dto.StartTime < r.EndTime) ||
                               (dto.EndTime > r.StartTime && dto.EndTime <= r.EndTime) ||
                               (dto.StartTime <= r.StartTime && dto.EndTime >= r.EndTime)));

            if (hasConflict)
                throw new Exception("This time slot is already reserved or pending approval");

            // Check for schedule conflicts (regular classes)
            var dayOfWeek = (int)dto.Date.DayOfWeek;
            var scheduleConflict = await _context.Schedules
                .AnyAsync(s => s.ClassroomId == dto.ClassroomId &&
                              s.DayOfWeek == dayOfWeek &&
                              s.IsActive &&
                              ((dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                               (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime) ||
                               (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)));

            if (scheduleConflict)
                throw new Exception("This time slot conflicts with a scheduled class");

            var reservation = new ClassroomReservation
            {
                ClassroomId = dto.ClassroomId,
                UserId = userId,
                Date = dto.Date.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Purpose = dto.Purpose,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassroomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return await MapToDto(reservation);
        }

        public async Task<List<ClassroomReservationDto>> GetReservationsAsync(int? classroomId = null, DateTime? date = null, string? status = null)
        {
            var query = _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.User)
                .Include(r => r.Approver)
                .AsQueryable();

            if (classroomId.HasValue)
                query = query.Where(r => r.ClassroomId == classroomId.Value);

            if (date.HasValue)
                query = query.Where(r => r.Date.Date == date.Value.Date);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reservations = await query
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            var result = new List<ClassroomReservationDto>();
            foreach (var r in reservations)
                result.Add(await MapToDto(r));
            return result;
        }

        public async Task<ClassroomReservationDto?> GetReservationByIdAsync(int id)
        {
            var reservation = await _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.User)
                .Include(r => r.Approver)
                .FirstOrDefaultAsync(r => r.Id == id);

            return reservation == null ? null : await MapToDto(reservation);
        }

        public async Task<List<ClassroomReservationDto>> GetMyReservationsAsync(int userId)
        {
            var reservations = await _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.Approver)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var result = new List<ClassroomReservationDto>();
            foreach (var r in reservations)
                result.Add(await MapToDto(r));
            return result;
        }

        public async Task<bool> CancelReservationAsync(int userId, int reservationId)
        {
            var reservation = await _context.ClassroomReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null)
                return false;

            if (reservation.Status == "cancelled")
                throw new Exception("Reservation already cancelled");

            if (reservation.Date.Date < DateTime.UtcNow.Date)
                throw new Exception("Cannot cancel past reservations");

            reservation.Status = "cancelled";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // ==================== APPROVAL WORKFLOW ====================

        public async Task<ClassroomReservationDto?> ApproveReservationAsync(int adminUserId, int reservationId, string? notes = null)
        {
            var reservation = await _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return null;

            if (reservation.Status != "pending")
                throw new Exception("Only pending reservations can be approved");

            reservation.Status = "approved";
            reservation.ApprovedBy = adminUserId;
            reservation.ReviewedAt = DateTime.UtcNow;
            reservation.Notes = notes;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToDto(reservation);
        }

        public async Task<ClassroomReservationDto?> RejectReservationAsync(int adminUserId, int reservationId, string? notes = null)
        {
            var reservation = await _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return null;

            if (reservation.Status != "pending")
                throw new Exception("Only pending reservations can be rejected");

            reservation.Status = "rejected";
            reservation.ApprovedBy = adminUserId;
            reservation.ReviewedAt = DateTime.UtcNow;
            reservation.Notes = notes;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToDto(reservation);
        }

        public async Task<List<ClassroomReservationDto>> GetPendingReservationsAsync()
        {
            var reservations = await _context.ClassroomReservations
                .Include(r => r.Classroom)
                .Include(r => r.User)
                .Where(r => r.Status == "pending")
                .OrderBy(r => r.Date)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            var result = new List<ClassroomReservationDto>();
            foreach (var r in reservations)
                result.Add(await MapToDto(r));
            return result;
        }

        // ==================== AVAILABILITY ====================

        public async Task<List<ClassroomAvailabilityDto>> GetClassroomAvailabilityAsync(int classroomId, DateTime date)
        {
            var slots = new List<ClassroomAvailabilityDto>();
            var dayOfWeek = (int)date.DayOfWeek;

            // Time slots: 08:00 - 20:00 (1 hour each)
            for (int hour = 8; hour < 20; hour++)
            {
                var startTime = new TimeSpan(hour, 0, 0);
                var endTime = new TimeSpan(hour + 1, 0, 0);

                // Check schedule conflicts
                var scheduleConflict = await _context.Schedules
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
                    .FirstOrDefaultAsync(s => s.ClassroomId == classroomId &&
                                  s.DayOfWeek == dayOfWeek &&
                                  s.IsActive &&
                                  startTime >= s.StartTime && startTime < s.EndTime);

                // Check reservation conflicts
                var reservationConflict = await _context.ClassroomReservations
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ClassroomId == classroomId &&
                                  r.Date.Date == date.Date &&
                                  (r.Status == "approved" || r.Status == "pending") &&
                                  startTime >= r.StartTime && startTime < r.EndTime);

                var slot = new ClassroomAvailabilityDto
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    IsAvailable = scheduleConflict == null && reservationConflict == null
                };

                if (scheduleConflict != null)
                {
                    slot.ReservedBy = "Scheduled Class";
                    slot.Purpose = scheduleConflict.Section?.Course?.Name ?? "Class";
                }
                else if (reservationConflict != null)
                {
                    slot.ReservedBy = reservationConflict.User != null 
                        ? $"{reservationConflict.User.FirstName} {reservationConflict.User.LastName}" 
                        : "Reserved";
                    slot.Purpose = reservationConflict.Purpose;
                }

                slots.Add(slot);
            }

            return slots;
        }

        public async Task<List<AvailableClassroomDto>> GetAvailableClassroomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var dayOfWeek = (int)date.DayOfWeek;

            // Get all classrooms
            var allClassrooms = await _context.Classrooms.ToListAsync();

            // Get busy classroom IDs from schedules
            var busyFromSchedules = await _context.Schedules
                .Where(s => s.DayOfWeek == dayOfWeek && s.IsActive &&
                           ((startTime >= s.StartTime && startTime < s.EndTime) ||
                            (endTime > s.StartTime && endTime <= s.EndTime) ||
                            (startTime <= s.StartTime && endTime >= s.EndTime)))
                .Select(s => s.ClassroomId)
                .ToListAsync();

            // Get busy classroom IDs from reservations
            var busyFromReservations = await _context.ClassroomReservations
                .Where(r => r.Date.Date == date.Date &&
                           (r.Status == "approved" || r.Status == "pending") &&
                           ((startTime >= r.StartTime && startTime < r.EndTime) ||
                            (endTime > r.StartTime && endTime <= r.EndTime) ||
                            (startTime <= r.StartTime && endTime >= r.EndTime)))
                .Select(r => r.ClassroomId)
                .ToListAsync();

            var busyClassroomIds = busyFromSchedules.Concat(busyFromReservations).Distinct().ToList();

            return allClassrooms
                .Where(c => !busyClassroomIds.Contains(c.Id))
                .Select(c => new AvailableClassroomDto
                {
                    Id = c.Id,
                    Building = c.Building,
                    RoomNumber = c.RoomNumber,
                    Capacity = c.Capacity
                })
                .OrderBy(c => c.Building)
                .ThenBy(c => c.RoomNumber)
                .ToList();
        }

        // ==================== HELPERS ====================

        private async Task<ClassroomReservationDto> MapToDto(ClassroomReservation r)
        {
            var user = r.User ?? await _context.Users.FindAsync(r.UserId);
            var approver = r.Approver ?? (r.ApprovedBy.HasValue ? await _context.Users.FindAsync(r.ApprovedBy.Value) : null);

            return new ClassroomReservationDto
            {
                Id = r.Id,
                ClassroomId = r.ClassroomId,
                ClassroomName = r.Classroom?.RoomNumber ?? "",
                Building = r.Classroom?.Building ?? "",
                UserId = r.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "",
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Purpose = r.Purpose,
                Status = r.Status,
                ApprovedByName = approver != null ? $"{approver.FirstName} {approver.LastName}" : null,
                ReviewedAt = r.ReviewedAt,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
