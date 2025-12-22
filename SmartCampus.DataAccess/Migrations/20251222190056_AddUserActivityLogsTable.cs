using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCampus.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tablo yoksa oluştur
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'UserActivityLogs';
                SET @preparedStatement = (SELECT IF(
                    (
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                        WHERE
                            (table_name = @tablename)
                            AND (table_schema = @dbname)
                    ) = 0,
                    'CREATE TABLE `UserActivityLogs` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `UserId` int NOT NULL,
                        `Action` longtext CHARACTER SET utf8mb4 NOT NULL,
                        `Description` longtext CHARACTER SET utf8mb4 NULL,
                        `IpAddress` longtext CHARACTER SET utf8mb4 NULL,
                        `Timestamp` datetime(6) NOT NULL,
                        `CreatedAt` datetime(6) NOT NULL,
                        `UpdatedAt` datetime(6) NULL,
                        `IsDeleted` tinyint(1) NOT NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_UserActivityLogs_UserId` (`UserId`),
                        CONSTRAINT `FK_UserActivityLogs_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
                    ) CHARACTER SET=utf8mb4;',
                    'SELECT 1'
                ));
                PREPARE stmt FROM @preparedStatement;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tablo yoksa silme işlemini atla
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'UserActivityLogs';
                SET @preparedStatement = (SELECT IF(
                    (
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                        WHERE
                            (table_name = @tablename)
                            AND (table_schema = @dbname)
                    ) > 0,
                    'DROP TABLE `UserActivityLogs`;',
                    'SELECT 1'
                ));
                PREPARE stmt FROM @preparedStatement;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
