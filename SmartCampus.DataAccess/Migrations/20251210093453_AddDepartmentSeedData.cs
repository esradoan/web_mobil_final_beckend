using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "FacultyName", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CENG", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Bilgisayar Mühendisliği", null },
                    { 2, "EE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Elektrik-Elektronik Mühendisliği", null },
                    { 3, "SE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mühendislik Fakültesi", false, "Yazılım Mühendisliği", null },
                    { 4, "BA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İktisadi ve İdari Bilimler Fakültesi", false, "İşletme", null },
                    { 5, "PSY", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fen-Edebiyat Fakültesi", false, "Psikoloji", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
