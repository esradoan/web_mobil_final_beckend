using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Part3_SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cafeterias",
                columns: new[] { "Id", "Capacity", "CreatedAt", "IsActive", "Location", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 500, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Ana Kampüs, A Blok Zemin Kat", "Merkez Yemekhane", null },
                    { 2, 200, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Mühendislik Fakültesi, B Blok", "Mühendislik Kafeteryası", null },
                    { 3, 100, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Merkez Kütüphane, 1. Kat", "Kütüphane Cafe", null }
                });

            // Events seed data kaldırıldı - Admin kullanıcısı oluşturulduktan sonra Program.cs'de ekleniyor
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cafeterias",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cafeterias",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Cafeterias",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
