using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartCampus.Entities;

namespace SmartCampus.DataAccess
{
    public class CampusDbContext : IdentityDbContext<User, Role, int>
    {
        public CampusDbContext(DbContextOptions<CampusDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Critical for Identity

            // User - Student (1:1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne() // User might not explicitly hold reference to Student in simple design, or add property in User
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Faculty (1:1)
            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.User)
                .WithOne()
                .HasForeignKey<Faculty>(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Department - Student (1:N)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Department)
                .WithMany()
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department - Faculty (1:N)
            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.Department)
                .WithMany()
                .HasForeignKey(f => f.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data for Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Bilgisayar Mühendisliği", Code = "CENG", FacultyName = "Mühendislik Fakültesi", CreatedAt = new System.DateTime(2024, 1, 1) },
                new Department { Id = 2, Name = "Elektrik-Elektronik Mühendisliği", Code = "EE", FacultyName = "Mühendislik Fakültesi", CreatedAt = new System.DateTime(2024, 1, 1) },
                new Department { Id = 3, Name = "Yazılım Mühendisliği", Code = "SE", FacultyName = "Mühendislik Fakültesi", CreatedAt = new System.DateTime(2024, 1, 1) },
                new Department { Id = 4, Name = "İşletme", Code = "BA", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = new System.DateTime(2024, 1, 1) },
                new Department { Id = 5, Name = "Psikoloji", Code = "PSY", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = new System.DateTime(2024, 1, 1) }
            );
        }
    }
}
