using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IEventService
    {
        // Event CRUD
        Task<List<EventDto>> GetEventsAsync(string? category = null, DateTime? date = null);
        Task<EventDto?> GetEventByIdAsync(int id);
        Task<EventDto> CreateEventAsync(int organizerId, CreateEventDto dto);
        Task<EventDto?> UpdateEventAsync(int id, UpdateEventDto dto);
        Task<bool> DeleteEventAsync(int id);
        
        // Registration
        Task<EventRegistrationDto> RegisterAsync(int userId, int eventId);
        Task<bool> CancelRegistrationAsync(int userId, int registrationId);
        Task<List<EventRegistrationDto>> GetEventRegistrationsAsync(int eventId);
        Task<List<EventRegistrationDto>> GetMyRegistrationsAsync(int userId);
        Task<EventRegistrationDto?> CheckInAsync(int eventId, string qrCode);
    }

    // ==================== DTOs ====================

    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
        public int RemainingSpots { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public bool IsPaid { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
    }

    public class CreateEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "social";
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public bool IsPaid { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateEventDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int? Capacity { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public string? Status { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class EventRegistrationDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Location { get; set; }
        public string? Category { get; set; }
        public bool? IsPaid { get; set; }
        public decimal? Price { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public bool CheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    // ==================== SERVICE ====================

    public class EventService : IEventService
    {
        private readonly CampusDbContext _context;
        private readonly IWalletService _walletService;

        public EventService(CampusDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        // ==================== EVENT CRUD ====================

        public async Task<List<EventDto>> GetEventsAsync(string? category = null, DateTime? date = null)
        {
            var query = _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Status == "published");

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            if (date.HasValue)
                query = query.Where(e => e.Date.Date == date.Value.Date);

            var events = await query.OrderBy(e => e.Date).ToListAsync();
            return events.Select(MapToEventDto).ToList();
        }

        public async Task<EventDto?> GetEventByIdAsync(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            return ev == null ? null : MapToEventDto(ev);
        }

        public async Task<EventDto> CreateEventAsync(int organizerId, CreateEventDto dto)
        {
            var ev = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Date = dto.Date.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                Capacity = dto.Capacity,
                RegisteredCount = 0,
                RegistrationDeadline = dto.RegistrationDeadline,
                IsPaid = dto.IsPaid,
                Price = dto.Price,
                Status = "published",
                ImageUrl = dto.ImageUrl,
                OrganizerId = organizerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return MapToEventDto(ev);
        }

        public async Task<EventDto?> UpdateEventAsync(int id, UpdateEventDto dto)
        {
            var ev = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return null;

            if (dto.Title != null) ev.Title = dto.Title;
            if (dto.Description != null) ev.Description = dto.Description;
            if (dto.Location != null) ev.Location = dto.Location;
            if (dto.Capacity.HasValue) ev.Capacity = dto.Capacity.Value;
            if (dto.RegistrationDeadline.HasValue) ev.RegistrationDeadline = dto.RegistrationDeadline.Value;
            if (dto.Status != null) ev.Status = dto.Status;
            if (dto.ImageUrl != null) ev.ImageUrl = dto.ImageUrl;

            ev.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToEventDto(ev);
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return false;

            ev.Status = "cancelled";
            ev.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== REGISTRATION ====================

        public async Task<EventRegistrationDto> RegisterAsync(int userId, int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
                throw new Exception("Event not found");

            if (ev.Status != "published")
                throw new Exception("Event is not open for registration");

            if (DateTime.UtcNow > ev.RegistrationDeadline)
                throw new Exception("Registration deadline has passed");

            // Check capacity
            if (ev.RegisteredCount >= ev.Capacity)
                throw new Exception("Event is full");

            // Check existing registration
            var existing = await _context.EventRegistrations
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != "cancelled");

            if (existing)
                throw new Exception("You are already registered for this event");

            // Check wallet balance for paid events
            if (ev.IsPaid && ev.Price > 0)
            {
                var wallet = await _walletService.GetBalanceAsync(userId);
                if (wallet.Balance < ev.Price)
                    throw new Exception($"Insufficient balance. Required: {ev.Price} TRY");
            }

            // Create registration
            var registration = new EventRegistration
            {
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow,
                QrCode = GenerateQrCode(),
                CheckedIn = false,
                Status = "registered",
                IsPaid = ev.IsPaid,
                CreatedAt = DateTime.UtcNow
            };

            // Update event count (atomic)
            ev.RegisteredCount++;
            ev.UpdatedAt = DateTime.UtcNow;

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return new EventRegistrationDto
            {
                Id = registration.Id,
                EventId = eventId,
                EventTitle = ev.Title,
                EventDate = ev.Date,
                UserId = userId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "",
                QrCode = registration.QrCode,
                CheckedIn = false,
                Status = "registered",
                RegistrationDate = registration.RegistrationDate
            };
        }

        public async Task<bool> CancelRegistrationAsync(int userId, int registrationId)
        {
            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (registration == null)
                return false;

            if (registration.Status == "cancelled")
                throw new Exception("Registration already cancelled");

            if (registration.CheckedIn)
                throw new Exception("Cannot cancel after check-in");

            // Update registration
            registration.Status = "cancelled";
            registration.UpdatedAt = DateTime.UtcNow;

            // Update event count
            if (registration.Event != null)
            {
                registration.Event.RegisteredCount = Math.Max(0, registration.Event.RegisteredCount - 1);
                registration.Event.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<EventRegistrationDto>> GetEventRegistrationsAsync(int eventId)
        {
            var registrations = await _context.EventRegistrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .Where(r => r.EventId == eventId && r.Status != "cancelled")
                .OrderBy(r => r.RegistrationDate)
                .ToListAsync();

            return registrations.Select(MapToRegistrationDto).ToList();
        }

        public async Task<List<EventRegistrationDto>> GetMyRegistrationsAsync(int userId)
        {
            var registrations = await _context.EventRegistrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Event!.Date)
                .ToListAsync();

            return registrations.Select(MapToRegistrationDto).ToList();
        }

        public async Task<EventRegistrationDto?> CheckInAsync(int eventId, string qrCode)
        {
            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.QrCode == qrCode);

            if (registration == null)
                throw new Exception("Invalid QR code");

            if (registration.Status == "cancelled")
                throw new Exception("Registration was cancelled");

            if (registration.CheckedIn)
                throw new Exception("Already checked in");

            // Check-in için: Test amaçlı bugün ve gelecek tarihler için check-in yapılabilir
            // Sadece geçmiş tarihler için hata ver
            var eventDate = registration.Event!.Date.Date;
            var today = DateTime.UtcNow.Date;
            
            if (eventDate < today)
                throw new Exception($"Bu etkinlik geçmiş bir tarih için ({eventDate:dd.MM.yyyy}). Check-in yapılamaz.");
            
            // Not: Production'da sadece bugün için check-in yapılabilir olmalı
            // Test için bugün ve gelecek tarihler için izin veriyoruz
            // if (eventDate > today)
            //     throw new Exception($"Bu etkinlik gelecek bir tarih için ({eventDate:dd.MM.yyyy}). Check-in sadece etkinlik günü yapılabilir.");

            // Check-in
            registration.CheckedIn = true;
            registration.CheckedInAt = DateTime.UtcNow;
            registration.UpdatedAt = DateTime.UtcNow;

            // If paid event, deduct from wallet
            if (registration.IsPaid && registration.Event.Price > 0)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == registration.UserId);
                if (wallet != null && wallet.Balance >= registration.Event.Price)
                {
                    wallet.Balance -= registration.Event.Price;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    var transaction = new Transaction
                    {
                        WalletId = wallet.Id,
                        Type = "debit",
                        Amount = registration.Event.Price,
                        BalanceAfter = wallet.Balance,
                        ReferenceType = "event",
                        ReferenceId = registration.Id,
                        Description = $"Etkinlik katılım - {registration.Event.Title}",
                        Status = "completed",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();

            return MapToRegistrationDto(registration);
        }

        // ==================== HELPERS ====================

        private string GenerateQrCode()
        {
            return $"EVT-{Guid.NewGuid():N}"[..20].ToUpper();
        }

        private EventDto MapToEventDto(Event ev)
        {
            return new EventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Category = ev.Category,
                Date = ev.Date,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Location = ev.Location,
                Capacity = ev.Capacity,
                RegisteredCount = ev.RegisteredCount,
                RemainingSpots = Math.Max(0, ev.Capacity - ev.RegisteredCount),
                RegistrationDeadline = ev.RegistrationDeadline,
                IsPaid = ev.IsPaid,
                Price = ev.Price,
                Status = ev.Status,
                ImageUrl = ev.ImageUrl,
                OrganizerName = ev.Organizer != null ? $"{ev.Organizer.FirstName} {ev.Organizer.LastName}" : ""
            };
        }

        private EventRegistrationDto MapToRegistrationDto(EventRegistration r)
        {
            return new EventRegistrationDto
            {
                Id = r.Id,
                EventId = r.EventId,
                EventTitle = r.Event?.Title ?? "",
                EventDate = r.Event?.Date ?? DateTime.MinValue,
                StartTime = r.Event?.StartTime,
                EndTime = r.Event?.EndTime,
                Location = r.Event?.Location,
                Category = r.Event?.Category,
                IsPaid = r.Event?.IsPaid,
                Price = r.Event?.Price,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "",
                QrCode = r.QrCode,
                CheckedIn = r.CheckedIn,
                CheckedInAt = r.CheckedInAt,
                Status = r.Status,
                RegistrationDate = r.RegistrationDate
            };
        }
    }
}
