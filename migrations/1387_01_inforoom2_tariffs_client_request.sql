CREATE TABLE internet.`inforoom2_tariff` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;


CREATE TABLE internet.`inforoom2_clientrequest` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`ApplicantName` VARCHAR(255) NULL DEFAULT NULL,
	`ApplicantPhoneNumber` INT(11) NULL DEFAULT NULL,
	`Email` VARCHAR(255) NULL DEFAULT NULL,
	`City` VARCHAR(255) NULL DEFAULT NULL,
	`Street` VARCHAR(255) NULL DEFAULT NULL,
	`House` INT(11) NULL DEFAULT NULL,
	`CaseHouse` INT(11) NULL DEFAULT NULL,
	`Apartment` INT(11) NULL DEFAULT NULL,
	`Floor` INT(11) NULL DEFAULT NULL,
	`Entrance` INT(11) NULL DEFAULT NULL,
	`tariff` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `Tariff` (`Tariff`),
	CONSTRAINT `FKB78978CE5FB0B963` FOREIGN KEY (`tariff`) REFERENCES internet.`inforoom2_tariff` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

