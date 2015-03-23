USE Internet;
CREATE TABLE `inforoom2_dealers` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Employee` INT(10) UNSIGNED NOT NULL,
	PRIMARY KEY (`Id`),
	UNIQUE INDEX `Employee` (`Employee`),
	CONSTRAINT `FK_Dealers` FOREIGN KEY (`Employee`) REFERENCES `partners` (`Id`) ON UPDATE NO ACTION ON DELETE NO ACTION
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
