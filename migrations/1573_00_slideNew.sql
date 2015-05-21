use internet;
ALTER TABLE `regions` ADD COLUMN `ShownOnMainPage` TINYINT NULL AFTER `_OfficeGeomark`;
ALTER TABLE `regions` ADD COLUMN `Parent` INT(10) NULL DEFAULT NULL AFTER `ShownOnMainPage`; 

ALTER TABLE `packagespeed` ADD COLUMN `IsPhysic` TINYINT NULL AFTER `Description`;

ALTER TABLE `tariffs` ADD COLUMN `Features` VARCHAR(255) NULL DEFAULT NULL AFTER `_IsArchived`;
ALTER TABLE `tariffs` ALTER `Description` DROP DEFAULT;
ALTER TABLE `tariffs` CHANGE COLUMN `Description` `Description` TEXT NULL AFTER `Name`;
ALTER TABLE `tariffs` ADD COLUMN `Published` TINYINT NULL DEFAULT NULL AFTER `Features`;

CREATE TABLE `inforoom2_slide` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Region` INT(10) UNSIGNED NULL DEFAULT NULL,
	`Url` TEXT NOT NULL,
	`ImagePath` TEXT NOT NULL,
	`LastEdit` DATE NOT NULL DEFAULT '0000-00-00',
	`Partner` INT(11) UNSIGNED NOT NULL DEFAULT '0',
	`Enabled` TINYINT NULL,
	`Priority` INT(11) NOT NULL DEFAULT '0',
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_slide_regions` (`Region`),
	INDEX `FK_inforoom2_slide_partners` (`Partner`),
	CONSTRAINT `FK_inforoom2_slide_partners` FOREIGN KEY (`Partner`) REFERENCES `partners` (`Id`),
	CONSTRAINT `FK_inforoom2_slide_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=12
;

CREATE TABLE `inforoom2_banner` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Region` INT(10) UNSIGNED NULL DEFAULT NULL,
	`Url` TEXT NOT NULL,
	`ImagePath` TEXT NOT NULL,
	`LastEdit` DATE NOT NULL DEFAULT '0000-00-00',
	`Partner` INT(11) UNSIGNED NOT NULL DEFAULT '0',
	`Enabled` TINYINT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_banner_regions` (`Region`),
	INDEX `FK_inforoom2_banner_partners` (`Partner`),
	CONSTRAINT `FK_inforoom2_banner_partners` FOREIGN KEY (`Partner`) REFERENCES `partners` (`Id`),
	CONSTRAINT `FK_inforoom2_banner_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=19

