using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;

namespace SmartCampus.API.Helpers
{
    /// <summary>
    /// Helper for applying Part 4 migration to existing databases.
    /// This is a workaround for databases that existed before Part 4 migration was created.
    /// For fresh databases, EF Core's standard Migrate() will handle everything.
    /// </summary>
    public static class DbMigrationHelper
    {
        private const string Part4MigrationId = "20251226152803_ImplementedPart4Entities";

        public static void ApplyPart4Migration(CampusDbContext context)
        {
            // Check if this is a fresh database (no Identity tables)
            if (!IsExistingDatabase(context))
            {
                Console.WriteLine("⚠️ Fresh database detected. Skipping manual Part 4 migration (EF Core will handle it).");
                return;
            }

            // Check if Part 4 migration already applied
            if (IsMigrationApplied(context, Part4MigrationId))
            {
                Console.WriteLine("✅ Part 4 migration already applied. Skipping.");
                return;
            }

            try
            {
                var sql = GetPart4MigrationSql();
                context.Database.ExecuteSqlRaw(sql);
                
                // Sync migration history
                SyncMigrationHistory(context);
                
                Console.WriteLine("✅ Part 4 Migration applied and History synced manually.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Migration application warning/error: {ex.Message}");
            }
        }

        private static bool IsExistingDatabase(CampusDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw("SELECT Id FROM `AspNetUsers` LIMIT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMigrationApplied(CampusDbContext context, string migrationId)
        {
            try
            {
                // Use EF Core's GetAppliedMigrations to check if migration is already applied
                var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
                return appliedMigrations.Contains(migrationId);
            }
            catch
            {
                // If we can't check, assume not applied (safer to apply than skip)
                return false;
            }
        }

        private static void SyncMigrationHistory(CampusDbContext context)
        {
            // Get all migrations from the Migrations folder dynamically
            var allMigrations = context.Database.GetMigrations().ToList();
            const string productVersion = "8.0.2";

            foreach (var migration in allMigrations)
            {
                // Use string interpolation with proper escaping
                var sql = $"INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('{migration}', '{productVersion}')";
                context.Database.ExecuteSqlRaw(sql);
            }
        }

        private static string GetPart4MigrationSql()
        {
            return @"
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

" + GetCreateIndexSql("NotificationPreferences", "IX_NotificationPreferences_UserId", "UNIQUE") +
GetCreateIndexSql("Notifications", "IX_Notifications_UserId", "") +
GetCreateIndexSql("SensorData", "IX_SensorData_SensorId", "") + @"

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251226152803_ImplementedPart4Entities', '8.0.2');
";
        }

        private static string GetCreateIndexSql(string tableName, string indexName, string indexType)
        {
            return $@"
SET @index_exists = (
    SELECT COUNT(*) FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = '{tableName}' 
    AND index_name = '{indexName}'
);
SET @sql = IF(@index_exists = 0, 
    'CREATE {indexType} INDEX `{indexName}` ON `{tableName}` (`{GetIndexColumnName(tableName, indexName)}`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
";
        }

        private static string GetIndexColumnName(string tableName, string indexName)
        {
            return indexName switch
            {
                "IX_NotificationPreferences_UserId" => "UserId",
                "IX_Notifications_UserId" => "UserId",
                "IX_SensorData_SensorId" => "SensorId",
                _ => "Id"
            };
        }
    }
}

