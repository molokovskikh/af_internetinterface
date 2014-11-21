CREATE TABLE internet.`inforoom2_newsblock` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Preview` VARCHAR(255) NULL DEFAULT NULL,
	`Body` VARCHAR(255) NULL DEFAULT NULL,
	`Title` VARCHAR(255) NULL DEFAULT NULL,
	`Url` VARCHAR(255) NULL DEFAULT NULL,
	`CreationDate` DATETIME NULL DEFAULT NULL,
	`PublishedDate` DATETIME NULL DEFAULT NULL,
	`IsPublished` TINYINT(1) NULL DEFAULT NULL,
	`Priority` INT(11) NULL DEFAULT NULL,
	`Employee` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `Employee` (`Employee`),
	CONSTRAINT `FK2E9848996583FF54` FOREIGN KEY (`Employee`) REFERENCES internet.`inforoom2_employee` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;