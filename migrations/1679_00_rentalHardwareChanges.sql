use internet;
ALTER TABLE `inforoom2_clientrentalhardware`
	ADD COLUMN `Name` VARCHAR(100) NULL DEFAULT NULL AFTER `Model`,
	ADD COLUMN `SerialNumber` VARCHAR(255) NULL DEFAULT NULL AFTER `Name`;

UPDATE internet.inforoom2_clientrentalhardware as ch 
INNER JOIN internet.inforoom2_hardwaremodels as hm ON ch.Model = hm.Id
SET ch.Name = IF( ISNULL(hm.Model),'',hm.Model),
 ch.SerialNumber = IF( ISNULL(hm.SerialNumber),'',hm.SerialNumber);
 
 
CREATE TABLE `inforoom2_hardwareparts` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(100) NULL DEFAULT NULL,
	`RentalHardware` INT(11) UNSIGNED NOT NULL,
	PRIMARY KEY (`Id`),
	INDEX `RentalHardwareTypeId` (`RentalHardware`),
	CONSTRAINT `RentalHardwareTypeId` FOREIGN KEY (`RentalHardware`) REFERENCES `inforoom2_rentalhardware` (`Id`) ON UPDATE CASCADE ON DELETE NO ACTION
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;
 CREATE TABLE `inforoom2_clienthardwareparts` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`PartId` INT(11) UNSIGNED NOT NULL,
	`ClientRentId` INT(11) UNSIGNED NOT NULL,
	`Absent` TINYINT(4) UNSIGNED NULL DEFAULT '0',
	PRIMARY KEY (`Id`),
	INDEX `ClientHardwareRentIdKey` (`PartId`),
	INDEX `ClientrentalhardwareRentId` (`ClientRentId`),
	CONSTRAINT `ClientHardwareRentIdKey` FOREIGN KEY (`PartId`) REFERENCES `inforoom2_hardwareparts` (`Id`) ON UPDATE CASCADE ON DELETE NO ACTION,
	CONSTRAINT `ClientrentalhardwareRentId` FOREIGN KEY (`ClientRentId`) REFERENCES `inforoom2_clientrentalhardware` (`Id`) ON UPDATE CASCADE ON DELETE NO ACTION
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;

ALTER TABLE `inforoom2_clientrentalhardware`
	ADD COLUMN `DeactivateComment` VARCHAR(255) NULL DEFAULT NULL AFTER `Comment`;