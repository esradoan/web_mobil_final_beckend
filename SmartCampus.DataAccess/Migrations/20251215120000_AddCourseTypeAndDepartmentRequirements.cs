using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTypeAndDepartmentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Type column to Courses table (enum as int)
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0); // 0 = Required

            // Add AllowCrossDepartment column to Courses table
            migrationBuilder.AddColumn<bool>(
                name: "AllowCrossDepartment",
                table: "Courses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Create DepartmentCourseRequirements table
            migrationBuilder.CreateTable(
                name: "DepartmentCourseRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MinimumGrade = table.Column<int>(type: "int", nullable: true),
                    RecommendedYear = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentCourseRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentCourseRequirements_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentCourseRequirements_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Create unique index on DepartmentId + CourseId
            migrationBuilder.CreateIndex(
                name: "IX_DepartmentCourseRequirements_DepartmentId_CourseId",
                table: "DepartmentCourseRequirements",
                columns: new[] { "DepartmentId", "CourseId" },
                unique: true);

            // Create index on CourseId for faster lookups
            migrationBuilder.CreateIndex(
                name: "IX_DepartmentCourseRequirements_CourseId",
                table: "DepartmentCourseRequirements",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop DepartmentCourseRequirements table
            migrationBuilder.DropTable(
                name: "DepartmentCourseRequirements");

            // Remove columns from Courses table
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AllowCrossDepartment",
                table: "Courses");
        }
    }
}

