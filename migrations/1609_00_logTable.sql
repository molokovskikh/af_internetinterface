USE internet;
CREATE TABLE `inforoom2_logs` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Employee` INT(11) UNSIGNED ZEROFILL NULL DEFAULT NULL,
	`Date` DATETIME NOT NULL,
	`Type` TINYINT(4) NOT NULL,
	`Message` TEXT NOT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;
