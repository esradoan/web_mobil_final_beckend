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

        // Part 1 - User Management
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
        
        // Part 2 - Academic Management
        public DbSet<Classroom> Classrooms { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; } = null!;
        public DbSet<CourseSection> CourseSections { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<DepartmentCourseRequirement> DepartmentCourseRequirements { get; set; } = null!;
        
        // Part 2 - Attendance System
        public DbSet<AttendanceSession> AttendanceSessions { get; set; } = null!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;
        public DbSet<ExcuseRequest> ExcuseRequests { get; set; } = null!;
        
        // Part 2 - Course Applications
        public DbSet<CourseApplication> CourseApplications { get; set; } = null!;
        
        // Part 2 - Student Course Applications
        public DbSet<StudentCourseApplication> StudentCourseApplications { get; set; } = null!;

        // Part 3 - Meal Service
        public DbSet<Cafeteria> Cafeterias { get; set; } = null!;
        public DbSet<MealMenu> MealMenus { get; set; } = null!;
        public DbSet<MealReservation> MealReservations { get; set; } = null!;
        
        // Part 3 - Wallet/Payment
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        
        // Part 3 - Event Management
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;
        
        // Part 3 - Scheduling & Reservations
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<ClassroomReservation> ClassroomReservations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Critical for Identity

            // ==================== PART 1 RELATIONSHIPS ====================
            
            // User - Student (1:1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
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

            // ==================== PART 2 RELATIONSHIPS ====================
            
            // Classroom - Unique constraint
            modelBuilder.Entity<Classroom>()
                .HasIndex(c => new { c.Building, c.RoomNumber })
                .IsUnique();

            // Course - Department (N:1)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // CoursePrerequisite - Composite Primary Key
            modelBuilder.Entity<CoursePrerequisite>()
                .HasKey(cp => new { cp.CourseId, cp.PrerequisiteCourseId });

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(cp => cp.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.PrerequisiteCourse)
                .WithMany(c => c.PrerequisiteFor)
                .HasForeignKey(cp => cp.PrerequisiteCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // DepartmentCourseRequirement - Department & Course
            modelBuilder.Entity<DepartmentCourseRequirement>()
                .HasOne(dcr => dcr.Department)
                .WithMany()
                .HasForeignKey(dcr => dcr.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentCourseRequirement>()
                .HasOne(dcr => dcr.Course)
                .WithMany(c => c.DepartmentRequirements)
                .HasForeignKey(dcr => dcr.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentCourseRequirement>()
                .HasIndex(dcr => new { dcr.DepartmentId, dcr.CourseId })
                .IsUnique();

            // CourseSection - Course (N:1)
            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Instructor)
                .WithMany()
                .HasForeignKey(cs => cs.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Classroom)
                .WithMany()
                .HasForeignKey(cs => cs.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CourseSection>()
                .HasIndex(cs => new { cs.CourseId, cs.SectionNumber, cs.Semester, cs.Year })
                .IsUnique();

            // Enrollment - Student & Section
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Section)
                .WithMany(cs => cs.Enrollments)
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.SectionId })
                .IsUnique();

            // AttendanceSession
            modelBuilder.Entity<AttendanceSession>()
                .HasOne(a => a.Section)
                .WithMany(cs => cs.AttendanceSessions)
                .HasForeignKey(a => a.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceSession>()
                .HasOne(a => a.Instructor)
                .WithMany()
                .HasForeignKey(a => a.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceSession>()
                .HasIndex(a => a.QrCode)
                .IsUnique();

            // AttendanceRecord
            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Session)
                .WithMany(a => a.Records)
                .HasForeignKey(ar => ar.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Student)
                .WithMany()
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(ar => new { ar.SessionId, ar.StudentId })
                .IsUnique();

            // ExcuseRequest
            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(er => er.Student)
                .WithMany()
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(er => er.Session)
                .WithMany(a => a.ExcuseRequests)
                .HasForeignKey(er => er.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(er => er.Reviewer)
                .WithMany()
                .HasForeignKey(er => er.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // CourseApplication
            modelBuilder.Entity<CourseApplication>()
                .HasOne(ca => ca.Course)
                .WithMany()
                .HasForeignKey(ca => ca.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseApplication>()
                .HasOne(ca => ca.Instructor)
                .WithMany()
                .HasForeignKey(ca => ca.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseApplication>()
                .HasOne(ca => ca.ProcessedByUser)
                .WithMany()
                .HasForeignKey(ca => ca.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CourseApplication>()
                .HasIndex(ca => new { ca.CourseId, ca.InstructorId })
                .IsUnique();

            // StudentCourseApplication
            modelBuilder.Entity<StudentCourseApplication>()
                .HasOne(sca => sca.Course)
                .WithMany()
                .HasForeignKey(sca => sca.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentCourseApplication>()
                .HasOne(sca => sca.Section)
                .WithMany()
                .HasForeignKey(sca => sca.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentCourseApplication>()
                .HasOne(sca => sca.Student)
                .WithMany()
                .HasForeignKey(sca => sca.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentCourseApplication>()
                .HasOne(sca => sca.ProcessedByUser)
                .WithMany()
                .HasForeignKey(sca => sca.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StudentCourseApplication>()
                .HasIndex(sca => new { sca.SectionId, sca.StudentId })
                .IsUnique();

            // ==================== PART 3 RELATIONSHIPS ====================

            // Cafeteria - MealMenu (1:N)
            modelBuilder.Entity<MealMenu>()
                .HasOne(m => m.Cafeteria)
                .WithMany(c => c.Menus)
                .HasForeignKey(m => m.CafeteriaId)
                .OnDelete(DeleteBehavior.Cascade);

            // MealReservation relationships
            modelBuilder.Entity<MealReservation>()
                .HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealReservation>()
                .HasOne(mr => mr.Menu)
                .WithMany(m => m.Reservations)
                .HasForeignKey(mr => mr.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MealReservation>()
                .HasOne(mr => mr.Cafeteria)
                .WithMany(c => c.Reservations)
                .HasForeignKey(mr => mr.CafeteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MealReservation>()
                .HasIndex(mr => mr.QrCode)
                .IsUnique();

            // Wallet - User (1:1)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne()
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            // Transaction - Wallet (N:1)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            // Event - Organizer (N:1)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            // EventRegistration relationships
            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(er => er.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.User)
                .WithMany()
                .HasForeignKey(er => er.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(er => er.QrCode)
                .IsUnique();

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(er => new { er.EventId, er.UserId })
                .IsUnique();

            // Schedule relationships
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Section)
                .WithMany()
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Classroom)
                .WithMany()
                .HasForeignKey(s => s.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new { s.ClassroomId, s.DayOfWeek, s.StartTime, s.Semester, s.Year })
                .IsUnique();

            // ClassroomReservation relationships
            modelBuilder.Entity<ClassroomReservation>()
                .HasOne(cr => cr.Classroom)
                .WithMany()
                .HasForeignKey(cr => cr.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassroomReservation>()
                .HasOne(cr => cr.User)
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassroomReservation>()
                .HasOne(cr => cr.Approver)
                .WithMany()
                .HasForeignKey(cr => cr.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ==================== SEED DATA ====================
            var seedDate = new System.DateTime(2024, 1, 1);
            
            // Departments Seed Data
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

            // Classrooms Seed Data (with GPS coordinates)
            modelBuilder.Entity<Classroom>().HasData(
                new Classroom { Id = 1, Building = "Mühendislik Fakültesi", RoomNumber = "A101", Capacity = 60, Latitude = 41.0082m, Longitude = 29.0389m, CreatedAt = seedDate },
                new Classroom { Id = 2, Building = "Mühendislik Fakültesi", RoomNumber = "A102", Capacity = 40, Latitude = 41.0083m, Longitude = 29.0390m, CreatedAt = seedDate },
                new Classroom { Id = 3, Building = "Mühendislik Fakültesi", RoomNumber = "B201", Capacity = 80, Latitude = 41.0084m, Longitude = 29.0391m, CreatedAt = seedDate },
                new Classroom { Id = 4, Building = "Fen-Edebiyat Fakültesi", RoomNumber = "C101", Capacity = 50, Latitude = 41.0085m, Longitude = 29.0392m, CreatedAt = seedDate },
                new Classroom { Id = 5, Building = "Fen-Edebiyat Fakültesi", RoomNumber = "C102", Capacity = 30, Latitude = 41.0086m, Longitude = 29.0393m, CreatedAt = seedDate }
            );

            // Courses Seed Data
            modelBuilder.Entity<Course>().HasData(
                // Bilgisayar Mühendisliği Dersleri (Zorunlu)
                new Course { Id = 1, Code = "CENG101", Name = "Programlamaya Giriş", Description = "Temel programlama kavramları", Credits = 4, Ects = 6, DepartmentId = 1, Type = CourseType.Required, AllowCrossDepartment = false, CreatedAt = seedDate },
                new Course { Id = 2, Code = "CENG102", Name = "Nesne Yönelimli Programlama", Description = "OOP kavramları", Credits = 4, Ects = 6, DepartmentId = 1, Type = CourseType.Required, AllowCrossDepartment = false, CreatedAt = seedDate },
                new Course { Id = 3, Code = "CENG201", Name = "Veri Yapıları", Description = "Temel veri yapıları ve algoritmalar", Credits = 4, Ects = 6, DepartmentId = 1, Type = CourseType.Required, AllowCrossDepartment = false, CreatedAt = seedDate },
                new Course { Id = 4, Code = "CENG301", Name = "Veritabanı Yönetim Sistemleri", Description = "SQL ve veritabanı tasarımı", Credits = 3, Ects = 5, DepartmentId = 1, Type = CourseType.Required, AllowCrossDepartment = false, CreatedAt = seedDate },
                new Course { Id = 5, Code = "CENG302", Name = "Web Programlama", Description = "Frontend ve backend geliştirme", Credits = 3, Ects = 5, DepartmentId = 1, Type = CourseType.Elective, AllowCrossDepartment = true, CreatedAt = seedDate },
                
                // Matematik Dersleri (Genel Seçmeli - Tüm bölümlerden alınabilir)
                new Course { Id = 6, Code = "MATH101", Name = "Matematik I", Description = "Kalkülüs I", Credits = 4, Ects = 6, DepartmentId = 16, Type = CourseType.GeneralElective, AllowCrossDepartment = true, CreatedAt = seedDate },
                new Course { Id = 7, Code = "MATH102", Name = "Matematik II", Description = "Kalkülüs II", Credits = 4, Ects = 6, DepartmentId = 16, Type = CourseType.GeneralElective, AllowCrossDepartment = true, CreatedAt = seedDate },
                new Course { Id = 8, Code = "MATH201", Name = "Lineer Cebir", Description = "Matrisler ve vektörler", Credits = 3, Ects = 5, DepartmentId = 16, Type = CourseType.GeneralElective, AllowCrossDepartment = true, CreatedAt = seedDate },
                
                // Fizik Dersleri (Genel Seçmeli)
                new Course { Id = 9, Code = "PHYS101", Name = "Fizik I", Description = "Mekanik", Credits = 4, Ects = 6, DepartmentId = 17, Type = CourseType.GeneralElective, AllowCrossDepartment = true, CreatedAt = seedDate },
                new Course { Id = 10, Code = "PHYS102", Name = "Fizik II", Description = "Elektrik ve Manyetizma", Credits = 4, Ects = 6, DepartmentId = 17, Type = CourseType.GeneralElective, AllowCrossDepartment = true, CreatedAt = seedDate }
            );

            // Course Prerequisites Seed Data
            modelBuilder.Entity<CoursePrerequisite>().HasData(
                new CoursePrerequisite { CourseId = 2, PrerequisiteCourseId = 1 },  // OOP requires Programlamaya Giriş
                new CoursePrerequisite { CourseId = 3, PrerequisiteCourseId = 2 },  // Veri Yapıları requires OOP
                new CoursePrerequisite { CourseId = 4, PrerequisiteCourseId = 3 },  // Veritabanı requires Veri Yapıları
                new CoursePrerequisite { CourseId = 5, PrerequisiteCourseId = 4 },  // Web Programlama requires Veritabanı
                new CoursePrerequisite { CourseId = 7, PrerequisiteCourseId = 6 },  // Matematik II requires Matematik I
                new CoursePrerequisite { CourseId = 8, PrerequisiteCourseId = 7 },  // Lineer Cebir requires Matematik II
                new CoursePrerequisite { CourseId = 10, PrerequisiteCourseId = 9 }  // Fizik II requires Fizik I
            );
        }
    }
}
