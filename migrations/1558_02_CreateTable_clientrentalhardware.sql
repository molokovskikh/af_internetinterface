USE Internet;
CREATE TABLE `inforoom2_clientrentalhardware` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Hardware` INT(11) UNSIGNED NOT NULL,
	`Model` INT(11) UNSIGNED NOT NULL,
	`Client` INT(11) UNSIGNED NOT NULL,
	`BeginDate` DATETIME NULL DEFAULT NULL,
	`EndDate` DATETIME NULL DEFAULT NULL,
	`Active` TINYINT(1) UNSIGNED NOT NULL DEFAULT '1',
	PRIMARY KEY (`Id`),
	INDEX `FK_CRH_Clients` (`Client`),
	INDEX `FK_CRH_HardwareModels` (`Model`),
	INDEX `FK_CRH_RentalHardware` (`Hardware`),
	CONSTRAINT `FK_CRH_RentalHardware` FOREIGN KEY (`Hardware`) REFERENCES `inforoom2_rentalhardware` (`Id`) ON UPDATE CASCADE ON DELETE NO ACTION
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
