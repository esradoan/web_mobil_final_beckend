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

        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;

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

            // Seed Data for Departments - Genişletilmiş Yelpaze
            var seedDate = new System.DateTime(2024, 1, 1);
            modelBuilder.Entity<Department>().HasData(
                // Mühendislik Fakültesi
                new Department { Id = 1, Name = "Bilgisayar Mühendisliği", Code = "CENG", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 2, Name = "Elektrik-Elektronik Mühendisliği", Code = "EE", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 3, Name = "Yazılım Mühendisliği", Code = "SE", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 6, Name = "Endüstri Mühendisliği", Code = "IE", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 7, Name = "Makine Mühendisliği", Code = "ME", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 8, Name = "İnşaat Mühendisliği", Code = "CE", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 9, Name = "Kimya Mühendisliği", Code = "CHE", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                new Department { Id = 10, Name = "Biyomedikal Mühendisliği", Code = "BME", FacultyName = "Mühendislik Fakültesi", CreatedAt = seedDate },
                
                // İktisadi ve İdari Bilimler Fakültesi
                new Department { Id = 4, Name = "İşletme", Code = "BA", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                new Department { Id = 11, Name = "İktisat", Code = "ECON", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                new Department { Id = 12, Name = "Siyaset Bilimi ve Kamu Yönetimi", Code = "POL", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                new Department { Id = 13, Name = "Uluslararası İlişkiler", Code = "IR", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                new Department { Id = 14, Name = "Maliye", Code = "FIN", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                new Department { Id = 15, Name = "Çalışma Ekonomisi ve Endüstri İlişkileri", Code = "LAB", FacultyName = "İktisadi ve İdari Bilimler Fakültesi", CreatedAt = seedDate },
                
                // Fen-Edebiyat Fakültesi
                new Department { Id = 5, Name = "Psikoloji", Code = "PSY", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 16, Name = "Matematik", Code = "MATH", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 17, Name = "Fizik", Code = "PHYS", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 18, Name = "Kimya", Code = "CHEM", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 19, Name = "Biyoloji", Code = "BIO", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 20, Name = "Türk Dili ve Edebiyatı", Code = "TURK", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 21, Name = "İngiliz Dili ve Edebiyatı", Code = "ENG", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 22, Name = "Tarih", Code = "HIST", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 23, Name = "Felsefe", Code = "PHIL", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                new Department { Id = 24, Name = "Sosyoloji", Code = "SOC", FacultyName = "Fen-Edebiyat Fakültesi", CreatedAt = seedDate },
                
                // Tıp Fakültesi
                new Department { Id = 25, Name = "Tıp", Code = "MED", FacultyName = "Tıp Fakültesi", CreatedAt = seedDate },
                new Department { Id = 26, Name = "Diş Hekimliği", Code = "DENT", FacultyName = "Tıp Fakültesi", CreatedAt = seedDate },
                new Department { Id = 27, Name = "Eczacılık", Code = "PHARM", FacultyName = "Tıp Fakültesi", CreatedAt = seedDate },
                new Department { Id = 28, Name = "Hemşirelik", Code = "NURS", FacultyName = "Tıp Fakültesi", CreatedAt = seedDate },
                new Department { Id = 29, Name = "Sağlık Bilimleri", Code = "HS", FacultyName = "Tıp Fakültesi", CreatedAt = seedDate },
                
                // Hukuk Fakültesi
                new Department { Id = 30, Name = "Hukuk", Code = "LAW", FacultyName = "Hukuk Fakültesi", CreatedAt = seedDate },
                
                // Eğitim Fakültesi
                new Department { Id = 31, Name = "Bilgisayar ve Öğretim Teknolojileri Eğitimi", Code = "CITE", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 32, Name = "Matematik Öğretmenliği", Code = "MATHED", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 33, Name = "Fen Bilgisi Öğretmenliği", Code = "SCIED", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 34, Name = "Türkçe Öğretmenliği", Code = "TURKED", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 35, Name = "İngilizce Öğretmenliği", Code = "ENGED", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 36, Name = "Okul Öncesi Öğretmenliği", Code = "PREED", FacultyName = "Eğitim Fakültesi", CreatedAt = seedDate },
                
                // İletişim Fakültesi
                new Department { Id = 37, Name = "Gazetecilik", Code = "JOUR", FacultyName = "İletişim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 38, Name = "Radyo, Televizyon ve Sinema", Code = "RTV", FacultyName = "İletişim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 39, Name = "Halkla İlişkiler ve Tanıtım", Code = "PR", FacultyName = "İletişim Fakültesi", CreatedAt = seedDate },
                new Department { Id = 40, Name = "Reklamcılık", Code = "ADV", FacultyName = "İletişim Fakültesi", CreatedAt = seedDate },
                
                // Güzel Sanatlar Fakültesi
                new Department { Id = 41, Name = "Grafik Tasarım", Code = "GD", FacultyName = "Güzel Sanatlar Fakültesi", CreatedAt = seedDate },
                new Department { Id = 42, Name = "Endüstriyel Tasarım", Code = "ID", FacultyName = "Güzel Sanatlar Fakültesi", CreatedAt = seedDate },
                new Department { Id = 43, Name = "Müzik", Code = "MUSIC", FacultyName = "Güzel Sanatlar Fakültesi", CreatedAt = seedDate },
                new Department { Id = 44, Name = "Resim", Code = "PAINT", FacultyName = "Güzel Sanatlar Fakültesi", CreatedAt = seedDate },
                
                // Mimarlık Fakültesi
                new Department { Id = 45, Name = "Mimarlık", Code = "ARCH", FacultyName = "Mimarlık Fakültesi", CreatedAt = seedDate },
                new Department { Id = 46, Name = "Şehir ve Bölge Planlama", Code = "URP", FacultyName = "Mimarlık Fakültesi", CreatedAt = seedDate },
                new Department { Id = 47, Name = "İç Mimarlık", Code = "INTARCH", FacultyName = "Mimarlık Fakültesi", CreatedAt = seedDate }
            );
        }
    }
}
