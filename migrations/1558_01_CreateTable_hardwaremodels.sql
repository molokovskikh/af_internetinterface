USE Internet;
CREATE TABLE `inforoom2_hardwaremodels` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Hardware` INT(11) UNSIGNED NOT NULL,
	`Model` VARCHAR(100) NULL DEFAULT NULL,
	`SerialNumber` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK_ModelHardware` (`Hardware`),
	CONSTRAINT `FK_Models_Hardware` FOREIGN KEY (`Hardware`) REFERENCES `inforoom2_rentalhardware` (`Id`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
