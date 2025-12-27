START TRANSACTION;

CREATE TABLE `IoTSensors` (
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

CREATE TABLE `NotificationPreferences` (
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

CREATE TABLE `Notifications` (
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

CREATE TABLE `SensorData` (
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

CREATE UNIQUE INDEX `IX_NotificationPreferences_UserId` ON `NotificationPreferences` (`UserId`);

CREATE INDEX `IX_Notifications_UserId` ON `Notifications` (`UserId`);

CREATE INDEX `IX_SensorData_SensorId` ON `SensorData` (`SensorId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251226152803_ImplementedPart4Entities', '8.0.2');

COMMIT;

