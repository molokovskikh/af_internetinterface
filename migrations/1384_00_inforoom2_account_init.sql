CREATE TABLE internet.`inforoom2_client` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Username` VARCHAR(255) NULL DEFAULT NULL,
	`Password` VARCHAR(255) NULL DEFAULT NULL,
	`Salt` VARCHAR(255) NULL DEFAULT NULL,
	`City` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=2;


CREATE TABLE internet.`inforoom2_employee` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Username` VARCHAR(255) NULL DEFAULT NULL,
	`Password` VARCHAR(255) NULL DEFAULT NULL,
	`Salt` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=2;

CREATE TABLE internet.`inforoom2_permissions` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;


CREATE TABLE internet.`inforoom2_questions` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Text` VARCHAR(255) NULL DEFAULT NULL,
	`Email` VARCHAR(255) NULL DEFAULT NULL,
	`Date` DATETIME NULL DEFAULT NULL,
	`Answer` VARCHAR(255) NULL DEFAULT NULL,
	`AnswerDate` DATETIME NULL DEFAULT NULL,
	`Notified` TINYINT(1) NULL DEFAULT NULL,
	`Published` TINYINT(1) NULL DEFAULT NULL,
	`Priority` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_roles` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=2;

CREATE TABLE internet.`inforoom2_user_role` (
	`user` INT(11) NULL DEFAULT NULL,
	`role` INT(11),
	`permission` INT(11),
	INDEX `role` (`role`),
	INDEX `user` (`user`),
	INDEX `permission` (`permission`),
	CONSTRAINT `FK98962F172F2E5505` FOREIGN KEY (`role`) REFERENCES internet.`inforoom2_roles` (`Id`),
	CONSTRAINT `FK98962F1734E2BA77` FOREIGN KEY (`user`) REFERENCES internet.`inforoom2_employee` (`Id`),
	CONSTRAINT `FK98962F17C306973B` FOREIGN KEY (`permission`) REFERENCES internet.`inforoom2_permissions` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

CREATE TABLE internet.`inforoom2_perm_role` (
	`permission` INT(11) NULL DEFAULT NULL,
	`role` INT(11) NOT NULL,
	INDEX `role` (`role`),
	INDEX `permission` (`permission`),
	CONSTRAINT `FK6EE9C7DB2F2E5505` FOREIGN KEY (`role`) REFERENCES internet.`inforoom2_roles` (`Id`),
	CONSTRAINT `FK6EE9C7DBC306973B` FOREIGN KEY (`permission`) REFERENCES internet.`inforoom2_permissions` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;




