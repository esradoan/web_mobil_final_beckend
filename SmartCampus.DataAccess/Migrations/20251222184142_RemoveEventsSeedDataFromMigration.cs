using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEventsSeedDataFromMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Capacity", "Category", "CreatedAt", "Date", "Description", "EndTime", "ImageUrl", "IsPaid", "Location", "OrganizerId", "Price", "RegisteredCount", "RegistrationDeadline", "StartTime", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 500, "conference", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sektörün önde gelen şirketlerinin katılımıyla kariyer fırsatları", new TimeSpan(0, 17, 0, 0, 0), null, false, "Kongre Merkezi", 1, 0m, 0, new DateTime(2024, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 9, 0, 0, 0), "published", "Kariyer Günleri 2024", null },
                    { 2, 30, "workshop", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 4, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "ChatGPT ve LLM'ler üzerine uygulamalı workshop", new TimeSpan(0, 18, 0, 0, 0), null, true, "Bilgisayar Lab 3", 1, 50m, 0, new DateTime(2024, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 14, 0, 0, 0), "published", "Yapay Zeka Workshop", null },
                    { 3, 2000, "social", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Müzik, dans ve eğlence dolu bahar festivali", new TimeSpan(0, 22, 0, 0, 0), null, false, "Kampüs Bahçesi", 1, 0m, 0, new DateTime(2024, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 12, 0, 0, 0), "published", "Bahar Şenliği", null },
                    { 4, 200, "sports", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2024, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bölümler arası futbol turnuvası", new TimeSpan(0, 18, 0, 0, 0), null, false, "Spor Sahası", 1, 0m, 0, new DateTime(2024, 5, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 10, 0, 0, 0), "published", "Futbol Turnuvası", null }
                });
        }
    }
}
