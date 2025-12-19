using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Student_IsScholarship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScholarship",
                table: "Students",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScholarship",
                table: "Students");
        }
    }
}
