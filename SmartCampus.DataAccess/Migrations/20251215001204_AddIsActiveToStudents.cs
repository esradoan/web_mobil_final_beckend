using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add IsActive column if it doesn't exist (for safety)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'Students';
                SET @columnname = 'IsActive';
                SET @preparedStatement = (SELECT IF(
                  (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE
                      (table_name = @tablename)
                      AND (table_schema = @dbname)
                      AND (column_name = @columnname)
                  ) > 0,
                  'SELECT 1', -- Column exists, do nothing
                  CONCAT('ALTER TABLE `', @tablename, '` ADD `', @columnname, '` tinyint(1) NOT NULL DEFAULT TRUE')
                ));
                PREPARE alterIfNotExists FROM @preparedStatement;
                EXECUTE alterIfNotExists;
                DEALLOCATE PREPARE alterIfNotExists;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Students");
        }
    }
}
