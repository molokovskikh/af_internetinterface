use internet;
CREATE TABLE `inforoom2_publicdata` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NOT NULL DEFAULT '0',
	`Region` INT(10) UNSIGNED NULL DEFAULT NULL,
	`LastUpdate` DATETIME NOT NULL,
	`ItemType` TINYINT(4) NOT NULL DEFAULT '0',
	`Display` TINYINT(1) NOT NULL DEFAULT '0',
	`RowIndex` INT(11) UNSIGNED NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_publicdata_regions` (`Region`),
	CONSTRAINT `FK_inforoom2_publicdata_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`)
);

CREATE TABLE `inforoom2_publicdatacontext` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`ParentId` INT(11) UNSIGNED NOT NULL,
	`Content` TEXT NOT NULL,
	`RowIndex` INT(11) UNSIGNED NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK__inforoom2_publicdata` (`ParentId`),
	CONSTRAINT `FK__inforoom2_publicdata` FOREIGN KEY (`ParentId`) REFERENCES `inforoom2_publicdata` (`Id`) ON UPDATE CASCADE ON DELETE CASCADE
);