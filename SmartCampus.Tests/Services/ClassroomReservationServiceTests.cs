using Microsoft.EntityFrameworkCore;
using Moq;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class ClassroomReservationServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly ClassroomReservationService _service;

        public ClassroomReservationServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _service = new ClassroomReservationService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldCreate_WhenSlotAvailable()
        {
            // Arrange
            var classroomId = 1;
            var userId = 100;
            _context.Classrooms.Add(new Classroom { Id = classroomId, RoomNumber = "101", Building = "A" });
            await _context.SaveChangesAsync();

            var dto = new CreateClassroomReservationDto
            {
                ClassroomId = classroomId,
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(11),
                Purpose = "Study"
            };

            // Act
            var result = await _service.CreateReservationAsync(userId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("pending", result.Status);
            Assert.Equal(dto.ClassroomId, result.ClassroomId);
            
            var dbRes = await _context.ClassroomReservations.FindAsync(result.Id);
            Assert.NotNull(dbRes);
        }

        [Fact]
        public async Task CreateReservationAsync_ShouldFail_WhenConflictExists()
        {
            // Arrange
            var classroomId = 2;
            _context.Classrooms.Add(new Classroom { Id = classroomId });
            
            // Existing reservation 10:00 - 12:00
            _context.ClassroomReservations.Add(new ClassroomReservation 
            { 
                ClassroomId = classroomId, 
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12),
                Status = "approved"
            });
            await _context.SaveChangesAsync();

            var dto = new CreateClassroomReservationDto
            {
                ClassroomId = classroomId,
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = TimeSpan.FromHours(11), // Overlap
                EndTime = TimeSpan.FromHours(13),
                Purpose = "Conflict"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateReservationAsync(101, dto));
        }

        [Fact]
        public async Task GetClassroomAvailabilityAsync_ShouldReturnSlots()
        {
            // Arrange
            var classroomId = 33; // Avoid seeded 1-5
            var date = DateTime.UtcNow.Date.AddDays(1);
            
            var course = new Course { Id = 300, Name="Test C", Code="TC" };
            var section = new CourseSection { Id = 301, CourseId = 300 };
            _context.Courses.Add(course);
            _context.CourseSections.Add(section);

            // Schedule: 09:00 - 10:00 (Class)
            _context.Schedules.Add(new Schedule 
            { 
                ClassroomId = classroomId, 
                DayOfWeek = (int)date.DayOfWeek, 
                StartTime = TimeSpan.FromHours(9), 
                EndTime = TimeSpan.FromHours(10),
                IsActive = true,
                SectionId = 301 // Linked to section
            });

            // Reservation: 14:00 - 15:00 (Study)
            var user = new User { Id = 50, FirstName="U", LastName="1" };
            _context.Users.Add(user);
            
            _context.ClassroomReservations.Add(new ClassroomReservation
            {
                ClassroomId = classroomId,
                UserId = 50,
                Date = date,
                StartTime = TimeSpan.FromHours(14),
                EndTime = TimeSpan.FromHours(15),
                Status = "approved",
                Purpose = "Study Group"
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetClassroomAvailabilityAsync(classroomId, date);

            // Assert
            // Slots are 08:00 to 20:00 (12 slots)
            Assert.Equal(12, result.Count);

            // 09:00-10:00 should be busy (Schedule)
            var slot9 = result.Find(s => s.StartTime == TimeSpan.FromHours(9));
            Assert.False(slot9.IsAvailable);
            Assert.Equal("Scheduled Class", slot9.ReservedBy);

            // 14:00-15:00 should be busy (Reservation)
            var slot14 = result.Find(s => s.StartTime == TimeSpan.FromHours(14));
            Assert.False(slot14.IsAvailable);
            Assert.Contains("Study Group", slot14.Purpose); // Use Contains to be safe

            // 10:00-11:00 should be available
            var slot10 = result.Find(s => s.StartTime == TimeSpan.FromHours(10));
            Assert.True(slot10.IsAvailable);
        }

        [Fact]
        public async Task ApproveReservationAsync_ShouldUpdateStatus()
        {
            // Arrange
            var resId = 40;
            var adminId = 999;
            var userId = 1;
            
            _context.Users.Add(new User { Id = userId, FirstName="Test", LastName="User" });
            _context.Classrooms.Add(new Classroom { Id = 10, RoomNumber = "101" });
            
            _context.ClassroomReservations.Add(new ClassroomReservation 
            { 
                Id = resId, 
                ClassroomId = 10,
                Status = "pending",
                UserId = userId,
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(11)
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ApproveReservationAsync(adminId, resId, "Approved");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("approved", result.Status);
            
            var dbRes = await _context.ClassroomReservations.FindAsync(resId);
            Assert.Equal("approved", dbRes.Status);
            Assert.Equal(adminId, dbRes.ApprovedBy);
        }
        [Fact]
        public async Task GetReservationsAsync_ShouldReturnFilteredReservations()
        {
            // Arrange
            var classroomId = 55; // Avoid seeded 1-5
            _context.Classrooms.Add(new Classroom { Id = classroomId });
            _context.Users.Add(new User { Id = 501, FirstName = "U", LastName = "1" });
            
            _context.ClassroomReservations.AddRange(
                new ClassroomReservation { Id = 51, ClassroomId = classroomId, UserId = 501, Date = DateTime.UtcNow.Date, Status = "approved" },
                new ClassroomReservation { Id = 52, ClassroomId = classroomId, UserId = 501, Date = DateTime.UtcNow.Date.AddDays(1), Status = "pending" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetReservationsAsync(classroomId: classroomId, status: "approved");

            // Assert
            Assert.Single(result);
            Assert.Equal(51, result[0].Id);
        }

        [Fact]
        public async Task GetMyReservationsAsync_ShouldReturnUserReservations()
        {
            // Arrange
            var userId = 600;
            _context.Users.Add(new User { Id = userId });
            _context.Classrooms.Add(new Classroom { Id = 6 });
            
            _context.ClassroomReservations.AddRange(
                new ClassroomReservation { Id = 61, UserId = userId, ClassroomId = 6, Date = DateTime.UtcNow },
                new ClassroomReservation { Id = 62, UserId = 601, ClassroomId = 6, Date = DateTime.UtcNow } // Other user
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMyReservationsAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal(61, result[0].Id);
        }

        [Fact]
        public async Task CancelReservationAsync_ShouldCancel_WhenValid()
        {
            // Arrange
            var userId = 700;
            var resId = 71;
            
            _context.Users.Add(new User { Id = userId });
            
            _context.ClassroomReservations.Add(new ClassroomReservation 
            { 
                Id = resId, 
                UserId = userId,
                Status = "pending",
                Date = DateTime.UtcNow.Date.AddDays(1) // Future date
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CancelReservationAsync(userId, resId);

            // Assert
            Assert.True(result);
            var dbRes = await _context.ClassroomReservations.FindAsync(resId);
            Assert.Equal("cancelled", dbRes.Status);
        }

        [Fact]
        public async Task RejectReservationAsync_ShouldReject_WhenPending()
        {
             // Arrange
            var resId = 80;
            var adminId = 999;
            var classroomId = 81; // Avoid 1-5

            _context.Users.Add(new User { Id = 1 });
            _context.Classrooms.Add(new Classroom { Id = classroomId });

            _context.ClassroomReservations.Add(new ClassroomReservation 
            { 
                Id = resId, 
                Status = "pending",
                UserId = 1,
                ClassroomId = classroomId
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RejectReservationAsync(adminId, resId, "Rejected");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("rejected", result.Status);
            
            var dbRes = await _context.ClassroomReservations.FindAsync(resId);
            Assert.Equal("rejected", dbRes.Status);
        }

        [Fact]
        public async Task GetPendingReservationsAsync_ShouldReturnPending()
        {
            // Arrange
            var classroomId = 91; // Avoid 1-5
            _context.Users.Add(new User { Id = 1 });
            _context.Classrooms.Add(new Classroom { Id = classroomId });
            
            _context.ClassroomReservations.AddRange(
                new ClassroomReservation { Id = 91, Status = "pending", UserId = 1, ClassroomId = classroomId },
                new ClassroomReservation { Id = 92, Status = "approved", UserId = 1, ClassroomId = classroomId }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetPendingReservationsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(91, result[0].Id);
        }

        [Fact]
        public async Task GetAvailableClassroomsAsync_ShouldReturnAvailable()
        {
            // Arrange
            var date = DateTime.UtcNow.Date.AddDays(1);
            var startTime = TimeSpan.FromHours(10);
            var endTime = TimeSpan.FromHours(11);
            
            var c1 = new Classroom { Id = 101, RoomNumber = "101", Building = "A" };
            var c2 = new Classroom { Id = 102, RoomNumber = "102", Building = "A" };
            _context.Classrooms.AddRange(c1, c2);

            // C1 is booked
            _context.ClassroomReservations.Add(new ClassroomReservation
            {
                ClassroomId = 101,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                Status = "approved"
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAvailableClassroomsAsync(date, startTime, endTime);

            // Assert
            // Result should contain c2 (102) and any seeded classrooms that are not booked
            // Seeded classrooms are 1-5. They are not booked in this test environment (unless seeded data includes reservations which it doesn't seem to)
            // So we should expect 1,2,3,4,5 + 102 (total 6).
            // But verify logical correctness: C1 (101) should NOT be there. C2 (102) SHOULD be there.
            
            Assert.Contains(result, c => c.Id == 102);
            Assert.DoesNotContain(result, c => c.Id == 101);
        }
    }
}
