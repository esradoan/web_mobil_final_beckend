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
            // UserActivityLogs tablosu zaten mevcut, oluşturma işlemini atla
            // migrationBuilder.CreateTable(...) kaldırıldı

            // Department verileri zaten mevcut, INSERT işlemini SQL ile IGNORE kullanarak yapıyoruz
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO `Departments` (`Id`, `Code`, `CreatedAt`, `FacultyName`, `IsDeleted`, `Name`, `UpdatedAt`)
                VALUES 
                (1, 'CENG', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Bilgisayar Mühendisliği', NULL),
                (2, 'EE', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Elektrik-Elektronik Mühendisliği', NULL),
                (3, 'SE', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Yazılım Mühendisliği', NULL),
                (4, 'BA', '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'İşletme', NULL),
                (5, 'PSY', '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Psikoloji', NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // UserActivityLogs tablosu silinmeyecek (zaten mevcut)
            // migrationBuilder.DropTable(...) kaldırıldı

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
