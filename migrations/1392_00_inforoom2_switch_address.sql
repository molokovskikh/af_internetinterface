CREATE TABLE internet.`inforoom2_city` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_region` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	`City` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `City` (`City`),
	CONSTRAINT `FKBDB1C32BACFDF38` FOREIGN KEY (`City`) REFERENCES internet.`inforoom2_city` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_street` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	`District` VARCHAR(255) NULL DEFAULT NULL,
	`Region` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `Region` (`Region`),
	CONSTRAINT `FK9D9AC8157E5929D3` FOREIGN KEY (`Region`) REFERENCES `inforoom2_region` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_house` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Number` VARCHAR(255) NULL DEFAULT NULL,
	`Housing` VARCHAR(255) NULL DEFAULT NULL,
	`ApartmentAmount` INT(11) NULL DEFAULT NULL,
	`EntranceAmount` INT(11) NULL DEFAULT NULL,
	`Street` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `Street` (`Street`),
	CONSTRAINT `FKE3C3D25999D86442` FOREIGN KEY (`Street`) REFERENCES internet.`inforoom2_street` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_address` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Entrance` INT(11) NULL DEFAULT NULL,
	`Apartment` INT(11) NULL DEFAULT NULL,
	`Floor` INT(11) NULL DEFAULT NULL,
	`house` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `house` (`house`),
	CONSTRAINT `FKB78EB8D1DAD3C361` FOREIGN KEY (`house`) REFERENCES internet.`inforoom2_house` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_switch` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_switchaddress` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Start` VARCHAR(255) NULL DEFAULT NULL,
	`End` VARCHAR(255) NULL DEFAULT NULL,
	`Side` INT(11) NULL DEFAULT NULL,
	`Entrance` INT(11) NULL DEFAULT NULL,
	`Apartment` INT(11) NULL DEFAULT NULL,
	`Floor` INT(11) NULL DEFAULT NULL,
	`House` INT(11) NULL DEFAULT NULL,
	`Switch` INT(11) NULL DEFAULT NULL,
	`Street` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `House` (`House`),
	INDEX `Switch` (`Switch`),
	INDEX `Street` (`Street`),
	CONSTRAINT `FKCB476ABD5D6871C7` FOREIGN KEY (`Switch`) REFERENCES internet.`inforoom2_switch` (`Id`),
	CONSTRAINT `FKCB476ABD99D86442` FOREIGN KEY (`Street`) REFERENCES internet.`inforoom2_street` (`Id`),
	CONSTRAINT `FKCB476ABDDAD3C361` FOREIGN KEY (`House`) REFERENCES internet.`inforoom2_house` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_plan` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

DROP TABLE IF EXISTS internet.`inforoom2_clientrequest`;
DROP TABLE IF EXISTS internet.`inforoom2_tariff`;

CREATE TABLE internet.`inforoom2_region_plan` (
	`region` INT(11) NULL DEFAULT NULL,
	`Plan` INT(11) NOT NULL,
	INDEX `Plan` (`Plan`),
	INDEX `region` (`region`),
	CONSTRAINT `FKE1E8D3C17E5929D3` FOREIGN KEY (`region`) REFERENCES internet.`inforoom2_region` (`Id`),
	CONSTRAINT `FKE1E8D3C1AC7590E6` FOREIGN KEY (`Plan`) REFERENCES internet.`inforoom2_plan` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_clientrequest` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`ApplicantName` VARCHAR(255) NULL DEFAULT NULL,
	`ApplicantPhoneNumber` INT(11) NULL DEFAULT NULL,
	`Email` VARCHAR(255) NULL DEFAULT NULL,
	`Address` INT(11) NULL DEFAULT NULL,
	`Plan` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `Address` (`Address`),
	INDEX `Plan` (`Plan`),
	CONSTRAINT `FKB78978CEAC7590E6` FOREIGN KEY (`Plan`) REFERENCES `inforoom2_plan` (`Id`),
	CONSTRAINT `FKB78978CEAFDBB375` FOREIGN KEY (`Address`) REFERENCES `inforoom2_address` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;














