using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPart2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Building = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoomNumber = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FeaturesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    Ects = table.Column<int>(type: "int", nullable: false),
                    SyllabusUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    PrerequisiteCourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => new { x.CourseId, x.PrerequisiteCourseId });
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_PrerequisiteCourseId",
                        column: x => x.PrerequisiteCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SectionNumber = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Semester = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    EnrolledCount = table.Column<int>(type: "int", nullable: false),
                    ScheduleJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClassroomId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSections_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSections_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseSections_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    GeofenceRadius = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    QrCode = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QrCodeExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_CourseSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "CourseSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MidtermGrade = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    FinalGrade = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    HomeworkGrade = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    LetterGrade = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GradePoint = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_CourseSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "CourseSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DistanceFromCenter = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsFlagged = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FlagReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AttendanceSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcuseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcuseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_AspNetUsers_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_AttendanceSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Classrooms",
                columns: new[] { "Id", "Building", "Capacity", "CreatedAt", "FeaturesJson", "IsDeleted", "Latitude", "Longitude", "RoomNumber", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Mühendislik Fakültesi", 60, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, 41.0082m, 29.0389m, "A101", null },
                    { 2, "Mühendislik Fakültesi", 40, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, 41.0083m, 29.0390m, "A102", null },
                    { 3, "Mühendislik Fakültesi", 80, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, 41.0084m, 29.0391m, "B201", null },
                    { 4, "Fen-Edebiyat Fakültesi", 50, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, 41.0085m, 29.0392m, "C101", null },
                    { 5, "Fen-Edebiyat Fakültesi", 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, 41.0086m, 29.0393m, "C102", null }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CreatedAt", "Credits", "DepartmentId", "Description", "Ects", "IsDeleted", "Name", "SyllabusUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CENG101", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 1, "Temel programlama kavramları", 6, false, "Programlamaya Giriş", null, null },
                    { 2, "CENG102", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 1, "OOP kavramları", 6, false, "Nesne Yönelimli Programlama", null, null },
                    { 3, "CENG201", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 1, "Temel veri yapıları ve algoritmalar", 6, false, "Veri Yapıları", null, null },
                    { 4, "CENG301", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 1, "SQL ve veritabanı tasarımı", 5, false, "Veritabanı Yönetim Sistemleri", null, null },
                    { 5, "CENG302", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 1, "Frontend ve backend geliştirme", 5, false, "Web Programlama", null, null }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "FacultyName", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 6, "IE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Endüstri Mühendisliği", null },
                    { 7, "ME", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Makine Mühendisliği", null },
                    { 8, "CE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "İnşaat Mühendisliği", null },
                    { 9, "CHE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Kimya Mühendisliği", null },
                    { 10, "BME", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Biyomedikal Mühendisliği", null },
                    { 11, "ECON", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "İktisat", null },
                    { 12, "POL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "Siyaset Bilimi ve Kamu Yönetimi", null },
                    { 13, "IR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "Uluslararası İlişkiler", null },
                    { 14, "FIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "Maliye", null },
                    { 15, "LAB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "Çalışma Ekonomisi ve Endüstri İlişkileri", null },
                    { 16, "MATH", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Matematik", null },
                    { 17, "PHYS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Fizik", null },
                    { 18, "CHEM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Kimya", null },
                    { 19, "BIO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Biyoloji", null },
                    { 20, "TURK", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Türk Dili ve Edebiyatı", null },
                    { 21, "ENG", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "İngiliz Dili ve Edebiyatı", null },
                    { 22, "HIST", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Tarih", null },
                    { 23, "PHIL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Felsefe", null },
                    { 24, "SOC", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Sosyoloji", null },
                    { 25, "MED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tıp Fakültesi", false, "Tıp", null },
                    { 26, "DENT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tıp Fakültesi", false, "Diş Hekimliği", null },
                    { 27, "PHARM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tıp Fakültesi", false, "Eczacılık", null },
                    { 28, "NURS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tıp Fakültesi", false, "Hemşirelik", null },
                    { 29, "HS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tıp Fakültesi", false, "Sağlık Bilimleri", null },
                    { 30, "LAW", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hukuk Fakültesi", false, "Hukuk", null },
                    { 31, "CITE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "Bilgisayar ve Öğretim Teknolojileri Eğitimi", null },
                    { 32, "MATHED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "Matematik Öğretmenliği", null },
                    { 33, "SCIED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "Fen Bilgisi Öğretmenliği", null },
                    { 34, "TURKED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "Türkçe Öğretmenliği", null },
                    { 35, "ENGED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "İngilizce Öğretmenliği", null },
                    { 36, "PREED", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim Fakültesi", false, "Okul Öncesi Öğretmenliği", null },
                    { 37, "JOUR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İletişim Fakültesi", false, "Gazetecilik", null },
                    { 38, "RTV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İletişim Fakültesi", false, "Radyo, Televizyon ve Sinema", null },
                    { 39, "PR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İletişim Fakültesi", false, "Halkla İlişkiler ve Tanıtım", null },
                    { 40, "ADV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İletişim Fakültesi", false, "Reklamcılık", null },
                    { 41, "GD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güzel Sanatlar Fakültesi", false, "Grafik Tasarım", null },
                    { 42, "ID", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güzel Sanatlar Fakültesi", false, "Endüstriyel Tasarım", null },
                    { 43, "MUSIC", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güzel Sanatlar Fakültesi", false, "Müzik", null },
                    { 44, "PAINT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güzel Sanatlar Fakültesi", false, "Resim", null },
                    { 45, "ARCH", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mimarlık Fakültesi", false, "Mimarlık", null },
                    { 46, "URP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mimarlık Fakültesi", false, "Şehir ve Bölge Planlama", null },
                    { 47, "INTARCH", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mimarlık Fakültesi", false, "İç Mimarlık", null }
                });

            migrationBuilder.InsertData(
                table: "CoursePrerequisites",
                columns: new[] { "CourseId", "PrerequisiteCourseId" },
                values: new object[,]
                {
                    { 2, 1 },
                    { 3, 2 },
                    { 4, 3 },
                    { 5, 4 }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CreatedAt", "Credits", "DepartmentId", "Description", "Ects", "IsDeleted", "Name", "SyllabusUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { 6, "MATH101", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 16, "Kalkülüs I", 6, false, "Matematik I", null, null },
                    { 7, "MATH102", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 16, "Kalkülüs II", 6, false, "Matematik II", null, null },
                    { 8, "MATH201", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 16, "Matrisler ve vektörler", 5, false, "Lineer Cebir", null, null },
                    { 9, "PHYS101", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 17, "Mekanik", 6, false, "Fizik I", null, null },
                    { 10, "PHYS102", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 17, "Elektrik ve Manyetizma", 6, false, "Fizik II", null, null }
                });

            migrationBuilder.InsertData(
                table: "CoursePrerequisites",
                columns: new[] { "CourseId", "PrerequisiteCourseId" },
                values: new object[,]
                {
                    { 7, 6 },
                    { 8, 7 },
                    { 10, 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SessionId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_InstructorId",
                table: "AttendanceSessions",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_QrCode",
                table: "AttendanceSessions",
                column: "QrCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_SectionId",
                table: "AttendanceSessions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_Building_RoomNumber",
                table: "Classrooms",
                columns: new[] { "Building", "RoomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_PrerequisiteCourseId",
                table: "CoursePrerequisites",
                column: "PrerequisiteCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_ClassroomId",
                table: "CourseSections",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_CourseId_SectionNumber_Semester_Year",
                table: "CourseSections",
                columns: new[] { "CourseId", "SectionNumber", "Semester", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_InstructorId",
                table: "CourseSections",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SectionId",
                table: "Enrollments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_SectionId",
                table: "Enrollments",
                columns: new[] { "StudentId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_ReviewedBy",
                table: "ExcuseRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_SessionId",
                table: "ExcuseRequests",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_StudentId",
                table: "ExcuseRequests",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "ExcuseRequests");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");

            migrationBuilder.DropTable(
                name: "CourseSections");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 47);
        }
    }
}
