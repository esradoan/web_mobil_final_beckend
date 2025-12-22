DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `Departments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Code` longtext CHARACTER SET utf8mb4 NOT NULL,
        `FacultyName` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Departments` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `Users` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FirstName` longtext CHARACTER SET utf8mb4 NOT NULL,
        `LastName` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Email` longtext CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Role` int NOT NULL,
        `IsEmailVerified` tinyint(1) NOT NULL,
        `EmailVerificationToken` longtext CHARACTER SET utf8mb4 NULL,
        `RefreshToken` longtext CHARACTER SET utf8mb4 NULL,
        `RefreshTokenExpiryTime` datetime(6) NULL,
        `ProfilePictureUrl` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `Faculties` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `EmployeeNumber` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
        `DepartmentId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Faculties` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Faculties_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Faculties_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `PasswordResetTokens` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Token` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ExpiryDate` datetime(6) NOT NULL,
        `IsUsed` tinyint(1) NOT NULL,
        `UserId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_PasswordResetTokens` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PasswordResetTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `RefreshTokens` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Token` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ExpiryDate` datetime(6) NOT NULL,
        `IsRevoked` tinyint(1) NOT NULL,
        `UserId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE TABLE `Students` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `StudentNumber` longtext CHARACTER SET utf8mb4 NOT NULL,
        `DepartmentId` int NOT NULL,
        `GPA` decimal(65,30) NOT NULL,
        `CGPA` decimal(65,30) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Students` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Students_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Students_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE INDEX `IX_Faculties_DepartmentId` ON `Faculties` (`DepartmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Faculties_UserId` ON `Faculties` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE INDEX `IX_PasswordResetTokens_UserId` ON `PasswordResetTokens` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE INDEX `IX_Students_DepartmentId` ON `Students` (`DepartmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Students_UserId` ON `Students` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207114925_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251207114925_InitialCreate', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `Faculties` DROP FOREIGN KEY `FK_Faculties_Users_UserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `PasswordResetTokens` DROP FOREIGN KEY `FK_PasswordResetTokens_Users_UserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `RefreshTokens` DROP FOREIGN KEY `FK_RefreshTokens_Users_UserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `Students` DROP FOREIGN KEY `FK_Students_Users_UserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Users');
    ALTER TABLE `Users` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `Users` RENAME `AspNetUsers`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` RENAME COLUMN `Role` TO `AccessFailedCount`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` RENAME COLUMN `IsEmailVerified` TO `TwoFactorEnabled`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` MODIFY COLUMN `PasswordHash` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` MODIFY COLUMN `Email` varchar(256) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `EmailConfirmed` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `LockoutEnabled` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `LockoutEnd` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `PhoneNumberConfirmed` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD `UserName` varchar(256) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `AspNetUsers` ADD CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'AspNetUsers', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetRoles` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
        `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
        `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetUserClaims` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
        `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetUserLogins` (
        `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
        `UserId` int NOT NULL,
        CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
        CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetUserTokens` (
        `UserId` int NOT NULL,
        `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Value` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
        CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetRoleClaims` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RoleId` int NOT NULL,
        `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
        `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE TABLE `AspNetUserRoles` (
        `UserId` int NOT NULL,
        `RoleId` int NOT NULL,
        CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
        CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `Faculties` ADD CONSTRAINT `FK_Faculties_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `PasswordResetTokens` ADD CONSTRAINT `FK_PasswordResetTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `RefreshTokens` ADD CONSTRAINT `FK_RefreshTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    ALTER TABLE `Students` ADD CONSTRAINT `FK_Students_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251207131914_AddIdentity') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251207131914_AddIdentity', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251210093453_AddDepartmentSeedData') THEN


                    INSERT IGNORE INTO `Departments` (`Id`, `Code`, `CreatedAt`, `FacultyName`, `IsDeleted`, `Name`, `UpdatedAt`)
                    VALUES 
                    (1, 'CENG', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Bilgisayar Mühendisliği', NULL),
                    (2, 'EE', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Elektrik-Elektronik Mühendisliği', NULL),
                    (3, 'SE', '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Yazılım Mühendisliği', NULL),
                    (4, 'BA', '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'İşletme', NULL),
                    (5, 'PSY', '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Psikoloji', NULL);
                

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251210093453_AddDepartmentSeedData') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251210093453_AddDepartmentSeedData', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `Classrooms` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Building` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `RoomNumber` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Capacity` int NOT NULL,
        `Latitude` decimal(65,30) NOT NULL,
        `Longitude` decimal(65,30) NOT NULL,
        `FeaturesJson` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Classrooms` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `Courses` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NULL,
        `Credits` int NOT NULL,
        `Ects` int NOT NULL,
        `SyllabusUrl` longtext CHARACTER SET utf8mb4 NULL,
        `DepartmentId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Courses` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Courses_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `CoursePrerequisites` (
        `CourseId` int NOT NULL,
        `PrerequisiteCourseId` int NOT NULL,
        CONSTRAINT `PK_CoursePrerequisites` PRIMARY KEY (`CourseId`, `PrerequisiteCourseId`),
        CONSTRAINT `FK_CoursePrerequisites_Courses_CourseId` FOREIGN KEY (`CourseId`) REFERENCES `Courses` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_CoursePrerequisites_Courses_PrerequisiteCourseId` FOREIGN KEY (`PrerequisiteCourseId`) REFERENCES `Courses` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `CourseSections` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CourseId` int NOT NULL,
        `SectionNumber` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Semester` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Year` int NOT NULL,
        `InstructorId` int NOT NULL,
        `Capacity` int NOT NULL,
        `EnrolledCount` int NOT NULL,
        `ScheduleJson` longtext CHARACTER SET utf8mb4 NULL,
        `ClassroomId` int NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_CourseSections` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CourseSections_AspNetUsers_InstructorId` FOREIGN KEY (`InstructorId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CourseSections_Classrooms_ClassroomId` FOREIGN KEY (`ClassroomId`) REFERENCES `Classrooms` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_CourseSections_Courses_CourseId` FOREIGN KEY (`CourseId`) REFERENCES `Courses` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `AttendanceSessions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SectionId` int NOT NULL,
        `InstructorId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `StartTime` time(6) NOT NULL,
        `EndTime` time(6) NOT NULL,
        `Latitude` decimal(65,30) NOT NULL,
        `Longitude` decimal(65,30) NOT NULL,
        `GeofenceRadius` decimal(65,30) NOT NULL,
        `QrCode` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `QrCodeExpiry` datetime(6) NOT NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_AttendanceSessions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AttendanceSessions_AspNetUsers_InstructorId` FOREIGN KEY (`InstructorId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_AttendanceSessions_CourseSections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `CourseSections` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `Enrollments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `StudentId` int NOT NULL,
        `SectionId` int NOT NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `EnrollmentDate` datetime(6) NOT NULL,
        `MidtermGrade` decimal(65,30) NULL,
        `FinalGrade` decimal(65,30) NULL,
        `HomeworkGrade` decimal(65,30) NULL,
        `LetterGrade` longtext CHARACTER SET utf8mb4 NULL,
        `GradePoint` decimal(65,30) NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Enrollments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Enrollments_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Enrollments_CourseSections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `CourseSections` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `AttendanceRecords` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SessionId` int NOT NULL,
        `StudentId` int NOT NULL,
        `CheckInTime` datetime(6) NOT NULL,
        `Latitude` decimal(65,30) NOT NULL,
        `Longitude` decimal(65,30) NOT NULL,
        `DistanceFromCenter` decimal(65,30) NOT NULL,
        `IsFlagged` tinyint(1) NOT NULL,
        `FlagReason` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_AttendanceRecords` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AttendanceRecords_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_AttendanceRecords_AttendanceSessions_SessionId` FOREIGN KEY (`SessionId`) REFERENCES `AttendanceSessions` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE TABLE `ExcuseRequests` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `StudentId` int NOT NULL,
        `SessionId` int NOT NULL,
        `Reason` longtext CHARACTER SET utf8mb4 NOT NULL,
        `DocumentUrl` longtext CHARACTER SET utf8mb4 NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ReviewedBy` int NULL,
        `ReviewedAt` datetime(6) NULL,
        `Notes` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_ExcuseRequests` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ExcuseRequests_AspNetUsers_ReviewedBy` FOREIGN KEY (`ReviewedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_ExcuseRequests_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ExcuseRequests_AttendanceSessions_SessionId` FOREIGN KEY (`SessionId`) REFERENCES `AttendanceSessions` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `Classrooms` (`Id`, `Building`, `Capacity`, `CreatedAt`, `FeaturesJson`, `IsDeleted`, `Latitude`, `Longitude`, `RoomNumber`, `UpdatedAt`)
    VALUES (1, 'Mühendislik Fakültesi', 60, TIMESTAMP '2024-01-01 00:00:00', NULL, FALSE, 41.0082, 29.0389, 'A101', NULL),
    (2, 'Mühendislik Fakültesi', 40, TIMESTAMP '2024-01-01 00:00:00', NULL, FALSE, 41.0083, 29.039, 'A102', NULL),
    (3, 'Mühendislik Fakültesi', 80, TIMESTAMP '2024-01-01 00:00:00', NULL, FALSE, 41.0084, 29.0391, 'B201', NULL),
    (4, 'Fen-Edebiyat Fakültesi', 50, TIMESTAMP '2024-01-01 00:00:00', NULL, FALSE, 41.0085, 29.0392, 'C101', NULL),
    (5, 'Fen-Edebiyat Fakültesi', 30, TIMESTAMP '2024-01-01 00:00:00', NULL, FALSE, 41.0086, 29.0393, 'C102', NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `Courses` (`Id`, `Code`, `CreatedAt`, `Credits`, `DepartmentId`, `Description`, `Ects`, `IsDeleted`, `Name`, `SyllabusUrl`, `UpdatedAt`)
    VALUES (1, 'CENG101', TIMESTAMP '2024-01-01 00:00:00', 4, 1, 'Temel programlama kavramları', 6, FALSE, 'Programlamaya Giriş', NULL, NULL),
    (2, 'CENG102', TIMESTAMP '2024-01-01 00:00:00', 4, 1, 'OOP kavramları', 6, FALSE, 'Nesne Yönelimli Programlama', NULL, NULL),
    (3, 'CENG201', TIMESTAMP '2024-01-01 00:00:00', 4, 1, 'Temel veri yapıları ve algoritmalar', 6, FALSE, 'Veri Yapıları', NULL, NULL),
    (4, 'CENG301', TIMESTAMP '2024-01-01 00:00:00', 3, 1, 'SQL ve veritabanı tasarımı', 5, FALSE, 'Veritabanı Yönetim Sistemleri', NULL, NULL),
    (5, 'CENG302', TIMESTAMP '2024-01-01 00:00:00', 3, 1, 'Frontend ve backend geliştirme', 5, FALSE, 'Web Programlama', NULL, NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `Departments` (`Id`, `Code`, `CreatedAt`, `FacultyName`, `IsDeleted`, `Name`, `UpdatedAt`)
    VALUES (6, 'IE', TIMESTAMP '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Endüstri Mühendisliği', NULL),
    (7, 'ME', TIMESTAMP '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Makine Mühendisliği', NULL),
    (8, 'CE', TIMESTAMP '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'İnşaat Mühendisliği', NULL),
    (9, 'CHE', TIMESTAMP '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Kimya Mühendisliği', NULL),
    (10, 'BME', TIMESTAMP '2024-01-01 00:00:00', 'Mühendislik Fakültesi', FALSE, 'Biyomedikal Mühendisliği', NULL),
    (11, 'ECON', TIMESTAMP '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'İktisat', NULL),
    (12, 'POL', TIMESTAMP '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'Siyaset Bilimi ve Kamu Yönetimi', NULL),
    (13, 'IR', TIMESTAMP '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'Uluslararası İlişkiler', NULL),
    (14, 'FIN', TIMESTAMP '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'Maliye', NULL),
    (15, 'LAB', TIMESTAMP '2024-01-01 00:00:00', 'İktisadi ve İdari Bilimler Fakültesi', FALSE, 'Çalışma Ekonomisi ve Endüstri İlişkileri', NULL),
    (16, 'MATH', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Matematik', NULL),
    (17, 'PHYS', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Fizik', NULL),
    (18, 'CHEM', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Kimya', NULL),
    (19, 'BIO', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Biyoloji', NULL),
    (20, 'TURK', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Türk Dili ve Edebiyatı', NULL),
    (21, 'ENG', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'İngiliz Dili ve Edebiyatı', NULL),
    (22, 'HIST', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Tarih', NULL),
    (23, 'PHIL', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Felsefe', NULL),
    (24, 'SOC', TIMESTAMP '2024-01-01 00:00:00', 'Fen-Edebiyat Fakültesi', FALSE, 'Sosyoloji', NULL),
    (25, 'MED', TIMESTAMP '2024-01-01 00:00:00', 'Tıp Fakültesi', FALSE, 'Tıp', NULL),
    (26, 'DENT', TIMESTAMP '2024-01-01 00:00:00', 'Tıp Fakültesi', FALSE, 'Diş Hekimliği', NULL),
    (27, 'PHARM', TIMESTAMP '2024-01-01 00:00:00', 'Tıp Fakültesi', FALSE, 'Eczacılık', NULL),
    (28, 'NURS', TIMESTAMP '2024-01-01 00:00:00', 'Tıp Fakültesi', FALSE, 'Hemşirelik', NULL),
    (29, 'HS', TIMESTAMP '2024-01-01 00:00:00', 'Tıp Fakültesi', FALSE, 'Sağlık Bilimleri', NULL),
    (30, 'LAW', TIMESTAMP '2024-01-01 00:00:00', 'Hukuk Fakültesi', FALSE, 'Hukuk', NULL),
    (31, 'CITE', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'Bilgisayar ve Öğretim Teknolojileri Eğitimi', NULL),
    (32, 'MATHED', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'Matematik Öğretmenliği', NULL),
    (33, 'SCIED', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'Fen Bilgisi Öğretmenliği', NULL),
    (34, 'TURKED', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'Türkçe Öğretmenliği', NULL),
    (35, 'ENGED', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'İngilizce Öğretmenliği', NULL),
    (36, 'PREED', TIMESTAMP '2024-01-01 00:00:00', 'Eğitim Fakültesi', FALSE, 'Okul Öncesi Öğretmenliği', NULL),
    (37, 'JOUR', TIMESTAMP '2024-01-01 00:00:00', 'İletişim Fakültesi', FALSE, 'Gazetecilik', NULL),
    (38, 'RTV', TIMESTAMP '2024-01-01 00:00:00', 'İletişim Fakültesi', FALSE, 'Radyo, Televizyon ve Sinema', NULL),
    (39, 'PR', TIMESTAMP '2024-01-01 00:00:00', 'İletişim Fakültesi', FALSE, 'Halkla İlişkiler ve Tanıtım', NULL),
    (40, 'ADV', TIMESTAMP '2024-01-01 00:00:00', 'İletişim Fakültesi', FALSE, 'Reklamcılık', NULL),
    (41, 'GD', TIMESTAMP '2024-01-01 00:00:00', 'Güzel Sanatlar Fakültesi', FALSE, 'Grafik Tasarım', NULL),
    (42, 'ID', TIMESTAMP '2024-01-01 00:00:00', 'Güzel Sanatlar Fakültesi', FALSE, 'Endüstriyel Tasarım', NULL),
    (43, 'MUSIC', TIMESTAMP '2024-01-01 00:00:00', 'Güzel Sanatlar Fakültesi', FALSE, 'Müzik', NULL),
    (44, 'PAINT', TIMESTAMP '2024-01-01 00:00:00', 'Güzel Sanatlar Fakültesi', FALSE, 'Resim', NULL),
    (45, 'ARCH', TIMESTAMP '2024-01-01 00:00:00', 'Mimarlık Fakültesi', FALSE, 'Mimarlık', NULL),
    (46, 'URP', TIMESTAMP '2024-01-01 00:00:00', 'Mimarlık Fakültesi', FALSE, 'Şehir ve Bölge Planlama', NULL),
    (47, 'INTARCH', TIMESTAMP '2024-01-01 00:00:00', 'Mimarlık Fakültesi', FALSE, 'İç Mimarlık', NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `CoursePrerequisites` (`CourseId`, `PrerequisiteCourseId`)
    VALUES (2, 1),
    (3, 2),
    (4, 3),
    (5, 4);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `Courses` (`Id`, `Code`, `CreatedAt`, `Credits`, `DepartmentId`, `Description`, `Ects`, `IsDeleted`, `Name`, `SyllabusUrl`, `UpdatedAt`)
    VALUES (6, 'MATH101', TIMESTAMP '2024-01-01 00:00:00', 4, 16, 'Kalkülüs I', 6, FALSE, 'Matematik I', NULL, NULL),
    (7, 'MATH102', TIMESTAMP '2024-01-01 00:00:00', 4, 16, 'Kalkülüs II', 6, FALSE, 'Matematik II', NULL, NULL),
    (8, 'MATH201', TIMESTAMP '2024-01-01 00:00:00', 3, 16, 'Matrisler ve vektörler', 5, FALSE, 'Lineer Cebir', NULL, NULL),
    (9, 'PHYS101', TIMESTAMP '2024-01-01 00:00:00', 4, 17, 'Mekanik', 6, FALSE, 'Fizik I', NULL, NULL),
    (10, 'PHYS102', TIMESTAMP '2024-01-01 00:00:00', 4, 17, 'Elektrik ve Manyetizma', 6, FALSE, 'Fizik II', NULL, NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `CoursePrerequisites` (`CourseId`, `PrerequisiteCourseId`)
    VALUES (7, 6),
    (8, 7),
    (10, 9);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_AttendanceRecords_SessionId_StudentId` ON `AttendanceRecords` (`SessionId`, `StudentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_AttendanceRecords_StudentId` ON `AttendanceRecords` (`StudentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_AttendanceSessions_InstructorId` ON `AttendanceSessions` (`InstructorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_AttendanceSessions_QrCode` ON `AttendanceSessions` (`QrCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_AttendanceSessions_SectionId` ON `AttendanceSessions` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_Classrooms_Building_RoomNumber` ON `Classrooms` (`Building`, `RoomNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_CoursePrerequisites_PrerequisiteCourseId` ON `CoursePrerequisites` (`PrerequisiteCourseId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_Courses_Code` ON `Courses` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_Courses_DepartmentId` ON `Courses` (`DepartmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_CourseSections_ClassroomId` ON `CourseSections` (`ClassroomId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_CourseSections_CourseId_SectionNumber_Semester_Year` ON `CourseSections` (`CourseId`, `SectionNumber`, `Semester`, `Year`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_CourseSections_InstructorId` ON `CourseSections` (`InstructorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_Enrollments_SectionId` ON `Enrollments` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE UNIQUE INDEX `IX_Enrollments_StudentId_SectionId` ON `Enrollments` (`StudentId`, `SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_ExcuseRequests_ReviewedBy` ON `ExcuseRequests` (`ReviewedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_ExcuseRequests_SessionId` ON `ExcuseRequests` (`SessionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    CREATE INDEX `IX_ExcuseRequests_StudentId` ON `ExcuseRequests` (`StudentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251214103112_AddPart2Entities') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251214103112_AddPart2Entities', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215001204_AddIsActiveToStudents') THEN


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
                

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215001204_AddIsActiveToStudents') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251215001204_AddIsActiveToStudents', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    ALTER TABLE `Courses` ADD `Type` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    ALTER TABLE `Courses` ADD `AllowCrossDepartment` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    CREATE TABLE `DepartmentCourseRequirements` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `DepartmentId` int NOT NULL,
        `CourseId` int NOT NULL,
        `IsRequired` tinyint(1) NOT NULL,
        `MinimumGrade` int NULL,
        `RecommendedYear` int NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_DepartmentCourseRequirements` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_DepartmentCourseRequirements_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_DepartmentCourseRequirements_Courses_CourseId` FOREIGN KEY (`CourseId`) REFERENCES `Courses` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    CREATE UNIQUE INDEX `IX_DepartmentCourseRequirements_DepartmentId_CourseId` ON `DepartmentCourseRequirements` (`DepartmentId`, `CourseId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    CREATE INDEX `IX_DepartmentCourseRequirements_CourseId` ON `DepartmentCourseRequirements` (`CourseId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215120000_AddCourseTypeAndDepartmentRequirements') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251215120000_AddCourseTypeAndDepartmentRequirements', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215215747_ChangeSectionApplicationToCourseApplication') THEN

    CREATE TABLE `CourseApplications` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CourseId` int NOT NULL,
        `InstructorId` int NOT NULL,
        `Status` int NOT NULL,
        `ProcessedAt` datetime(6) NULL,
        `ProcessedBy` int NULL,
        `RejectionReason` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_CourseApplications` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CourseApplications_AspNetUsers_InstructorId` FOREIGN KEY (`InstructorId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CourseApplications_AspNetUsers_ProcessedBy` FOREIGN KEY (`ProcessedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_CourseApplications_Courses_CourseId` FOREIGN KEY (`CourseId`) REFERENCES `Courses` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215215747_ChangeSectionApplicationToCourseApplication') THEN

    CREATE UNIQUE INDEX `IX_CourseApplications_CourseId_InstructorId` ON `CourseApplications` (`CourseId`, `InstructorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215215747_ChangeSectionApplicationToCourseApplication') THEN

    CREATE INDEX `IX_CourseApplications_InstructorId` ON `CourseApplications` (`InstructorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215215747_ChangeSectionApplicationToCourseApplication') THEN

    CREATE INDEX `IX_CourseApplications_ProcessedBy` ON `CourseApplications` (`ProcessedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215215747_ChangeSectionApplicationToCourseApplication') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251215215747_ChangeSectionApplicationToCourseApplication', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    CREATE TABLE `StudentCourseApplications` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CourseId` int NOT NULL,
        `SectionId` int NOT NULL,
        `StudentId` int NOT NULL,
        `Status` int NOT NULL,
        `ProcessedAt` datetime(6) NULL,
        `ProcessedBy` int NULL,
        `RejectionReason` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        CONSTRAINT `PK_StudentCourseApplications` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_StudentCourseApplications_AspNetUsers_ProcessedBy` FOREIGN KEY (`ProcessedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_StudentCourseApplications_AspNetUsers_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_StudentCourseApplications_CourseSections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `CourseSections` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_StudentCourseApplications_Courses_CourseId` FOREIGN KEY (`CourseId`) REFERENCES `Courses` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    CREATE INDEX `IX_StudentCourseApplications_CourseId` ON `StudentCourseApplications` (`CourseId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    CREATE INDEX `IX_StudentCourseApplications_ProcessedBy` ON `StudentCourseApplications` (`ProcessedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    CREATE UNIQUE INDEX `IX_StudentCourseApplications_SectionId_StudentId` ON `StudentCourseApplications` (`SectionId`, `StudentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    CREATE INDEX `IX_StudentCourseApplications_StudentId` ON `StudentCourseApplications` (`StudentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251215222233_AddStudentCourseApplication') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251215222233_AddStudentCourseApplication', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `Cafeterias` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Location` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Capacity` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Cafeterias` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `ClassroomReservations` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ClassroomId` int NOT NULL,
        `UserId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `StartTime` time(6) NOT NULL,
        `EndTime` time(6) NOT NULL,
        `Purpose` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ApprovedBy` int NULL,
        `ReviewedAt` datetime(6) NULL,
        `Notes` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_ClassroomReservations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ClassroomReservations_AspNetUsers_ApprovedBy` FOREIGN KEY (`ApprovedBy`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_ClassroomReservations_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ClassroomReservations_Classrooms_ClassroomId` FOREIGN KEY (`ClassroomId`) REFERENCES `Classrooms` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `Events` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Category` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Date` datetime(6) NOT NULL,
        `StartTime` time(6) NOT NULL,
        `EndTime` time(6) NOT NULL,
        `Location` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Capacity` int NOT NULL,
        `RegisteredCount` int NOT NULL,
        `RegistrationDeadline` datetime(6) NOT NULL,
        `IsPaid` tinyint(1) NOT NULL,
        `Price` decimal(65,30) NOT NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ImageUrl` longtext CHARACTER SET utf8mb4 NULL,
        `OrganizerId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Events` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Events_AspNetUsers_OrganizerId` FOREIGN KEY (`OrganizerId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `Schedules` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SectionId` int NOT NULL,
        `DayOfWeek` int NOT NULL,
        `StartTime` time(6) NOT NULL,
        `EndTime` time(6) NOT NULL,
        `ClassroomId` int NOT NULL,
        `Semester` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Year` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Schedules` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Schedules_Classrooms_ClassroomId` FOREIGN KEY (`ClassroomId`) REFERENCES `Classrooms` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Schedules_CourseSections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `CourseSections` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `Wallets` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `Balance` decimal(65,30) NOT NULL,
        `Currency` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Wallets` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Wallets_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `MealMenus` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CafeteriaId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `MealType` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ItemsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `NutritionJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsPublished` tinyint(1) NOT NULL,
        `HasVegetarianOption` tinyint(1) NOT NULL,
        `Price` decimal(65,30) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_MealMenus` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_MealMenus_Cafeterias_CafeteriaId` FOREIGN KEY (`CafeteriaId`) REFERENCES `Cafeterias` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `EventRegistrations` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `EventId` int NOT NULL,
        `UserId` int NOT NULL,
        `RegistrationDate` datetime(6) NOT NULL,
        `QrCode` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `CheckedIn` tinyint(1) NOT NULL,
        `CheckedInAt` datetime(6) NULL,
        `CustomFieldsJson` longtext CHARACTER SET utf8mb4 NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsPaid` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_EventRegistrations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_EventRegistrations_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_EventRegistrations_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `Transactions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `WalletId` int NOT NULL,
        `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Amount` decimal(65,30) NOT NULL,
        `BalanceAfter` decimal(65,30) NOT NULL,
        `ReferenceType` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ReferenceId` int NULL,
        `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
        `PaymentReference` longtext CHARACTER SET utf8mb4 NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Transactions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Transactions_Wallets_WalletId` FOREIGN KEY (`WalletId`) REFERENCES `Wallets` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE TABLE `MealReservations` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `MenuId` int NOT NULL,
        `CafeteriaId` int NOT NULL,
        `MealType` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Date` datetime(6) NOT NULL,
        `Amount` decimal(65,30) NOT NULL,
        `QrCode` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `UsedAt` datetime(6) NULL,
        `IsScholarship` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_MealReservations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_MealReservations_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_MealReservations_Cafeterias_CafeteriaId` FOREIGN KEY (`CafeteriaId`) REFERENCES `Cafeterias` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_MealReservations_MealMenus_MenuId` FOREIGN KEY (`MenuId`) REFERENCES `MealMenus` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_ClassroomReservations_ApprovedBy` ON `ClassroomReservations` (`ApprovedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_ClassroomReservations_ClassroomId` ON `ClassroomReservations` (`ClassroomId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_ClassroomReservations_UserId` ON `ClassroomReservations` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE UNIQUE INDEX `IX_EventRegistrations_EventId_UserId` ON `EventRegistrations` (`EventId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE UNIQUE INDEX `IX_EventRegistrations_QrCode` ON `EventRegistrations` (`QrCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_EventRegistrations_UserId` ON `EventRegistrations` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_Events_OrganizerId` ON `Events` (`OrganizerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_MealMenus_CafeteriaId` ON `MealMenus` (`CafeteriaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_MealReservations_CafeteriaId` ON `MealReservations` (`CafeteriaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_MealReservations_MenuId` ON `MealReservations` (`MenuId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE UNIQUE INDEX `IX_MealReservations_QrCode` ON `MealReservations` (`QrCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_MealReservations_UserId` ON `MealReservations` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE UNIQUE INDEX `IX_Schedules_ClassroomId_DayOfWeek_StartTime_Semester_Year` ON `Schedules` (`ClassroomId`, `DayOfWeek`, `StartTime`, `Semester`, `Year`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_Schedules_SectionId` ON `Schedules` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE INDEX `IX_Transactions_WalletId` ON `Transactions` (`WalletId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    CREATE UNIQUE INDEX `IX_Wallets_UserId` ON `Wallets` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125158_Part3_MealEventScheduling') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251219125158_Part3_MealEventScheduling', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125510_Part3_SeedData') THEN

    INSERT INTO `Cafeterias` (`Id`, `Capacity`, `CreatedAt`, `IsActive`, `Location`, `Name`, `UpdatedAt`)
    VALUES (1, 500, TIMESTAMP '2024-01-01 00:00:00', TRUE, 'Ana Kampüs, A Blok Zemin Kat', 'Merkez Yemekhane', NULL),
    (2, 200, TIMESTAMP '2024-01-01 00:00:00', TRUE, 'Mühendislik Fakültesi, B Blok', 'Mühendislik Kafeteryası', NULL),
    (3, 100, TIMESTAMP '2024-01-01 00:00:00', TRUE, 'Merkez Kütüphane, 1. Kat', 'Kütüphane Cafe', NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219125510_Part3_SeedData') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251219125510_Part3_SeedData', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219130501_Student_IsScholarship') THEN

    ALTER TABLE `Students` ADD `IsScholarship` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219130501_Student_IsScholarship') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251219130501_Student_IsScholarship', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222184142_RemoveEventsSeedDataFromMigration') THEN

    DELETE FROM `Events`
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222184142_RemoveEventsSeedDataFromMigration') THEN

    DELETE FROM `Events`
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222184142_RemoveEventsSeedDataFromMigration') THEN

    DELETE FROM `Events`
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222184142_RemoveEventsSeedDataFromMigration') THEN

    DELETE FROM `Events`
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222184142_RemoveEventsSeedDataFromMigration') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251222184142_RemoveEventsSeedDataFromMigration', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222185221_AddUserActivityLogsTable') THEN


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
                

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251222185221_AddUserActivityLogsTable') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251222185221_AddUserActivityLogsTable', '8.0.2');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

DROP PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`;

DROP PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`;

