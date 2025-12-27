using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;

namespace SmartCampus.API.Helpers
{
    public static class DbMigrationHelper
    {
        public static void ApplyPart4Migration(CampusDbContext context)
        {
            try
            {
                // Check if migration already applied
                var historyExists = context.Database.ExecuteSqlRaw("SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251226152803_ImplementedPart4Entities'");
            }
            catch
            {
                // Ignore check errors
            }

            // SAFETY CHECK: If Identity tables don't exist, this is a FRESH database.
            // We should let EF Core's standard Migrate() handle everything in that case.
            // We only need this manual helper if we are "patching" an existing DB.
            try
            {
                // Check if AspNetUsers exists (MySQL syntax)
                var tableExists = context.Database.ExecuteSqlRaw("SELECT 1 FROM `AspNetUsers` LIMIT 1");
                // If this throws or returns 0 (though ExecuteSqlRaw returns affected rows not result), 
                // actually ExecuteSqlRaw is for commands. Querying is harder. 
                // Let's use a dummy query wrapped in try/catch.
                
                 context.Database.ExecuteSqlRaw("SELECT Id FROM `AspNetUsers` LIMIT 1");
            }
            catch
            {
                Console.WriteLine("⚠️ AspNetUsers table not found. Skipping manual Part 4 migration (Assuming Fresh DB).");
                return;
            }

            var sql = @"
CREATE TABLE IF NOT EXISTS `IoTSensors` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Location` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `LastValue` double NULL,
    `Unit` varchar(20) CHARACTER SET utf8mb4 NULL,
    `LastUpdate` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_IoTSensors` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `NotificationPreferences` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `EmailEnabled` tinyint(1) NOT NULL,
    `PushEnabled` tinyint(1) NOT NULL,
    `SmsEnabled` tinyint(1) NOT NULL,
    `AcademicNotifications` tinyint(1) NOT NULL,
    `AttendanceNotifications` tinyint(1) NOT NULL,
    `MealNotifications` tinyint(1) NOT NULL,
    `EventNotifications` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_NotificationPreferences` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_NotificationPreferences_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Notifications` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IsRead` tinyint(1) NOT NULL,
    `ReferenceType` longtext CHARACTER SET utf8mb4 NULL,
    `ReferenceId` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Notifications` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Notifications_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SensorData` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SensorId` int NOT NULL,
    `Value` double NOT NULL,
    `Unit` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_SensorData` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_SensorData_IoTSensors_SensorId` FOREIGN KEY (`SensorId`) REFERENCES `IoTSensors` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- Index creation: Check if exists before creating to avoid duplicate key errors
-- MySQL doesn't support IF NOT EXISTS for indexes, so we use a workaround

SET @index_exists = (
    SELECT COUNT(*) FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'NotificationPreferences' 
    AND index_name = 'IX_NotificationPreferences_UserId'
);
SET @sql = IF(@index_exists = 0, 
    'CREATE UNIQUE INDEX `IX_NotificationPreferences_UserId` ON `NotificationPreferences` (`UserId`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (
    SELECT COUNT(*) FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Notifications' 
    AND index_name = 'IX_Notifications_UserId'
);
SET @sql = IF(@index_exists = 0, 
    'CREATE INDEX `IX_Notifications_UserId` ON `Notifications` (`UserId`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (
    SELECT COUNT(*) FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'SensorData' 
    AND index_name = 'IX_SensorData_SensorId'
);
SET @sql = IF(@index_exists = 0, 
    'CREATE INDEX `IX_SensorData_SensorId` ON `SensorData` (`SensorId`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251226152803_ImplementedPart4Entities', '8.0.2');
";
            
            try
            {
                context.Database.ExecuteSqlRaw(sql);
                
                // Backfill previous migrations to prevent EF from trying to run them again
                var previousMigrations = new[]
                {
                    "20251207114925_InitialCreate",
                    "20251207131914_AddIdentity",
                    "20251210093453_AddDepartmentSeedData",
                    "20251214103112_AddPart2Entities",
                    "20251215001204_AddIsActiveToStudents",
                    "20251215120000_AddCourseTypeAndDepartmentRequirements",
                    "20251215215747_ChangeSectionApplicationToCourseApplication",
                    "20251215222233_AddStudentCourseApplication",
                    "20251219125158_Part3_MealEventScheduling",
                    "20251219125510_Part3_SeedData",
                    "20251219130501_Student_IsScholarship",
                    "20251222184142_RemoveEventsSeedDataFromMigration",
                    "20251222190056_AddUserActivityLogsTable"
                };

                foreach (var migration in previousMigrations)
                {
                    context.Database.ExecuteSqlRaw($"INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('{migration}', '8.0.2');");
                }
                
                Console.WriteLine("✅ Part 4 Migration applied and History synced manually.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration application warning/error: {ex.Message}");
            }
        }
    }
}

