use internet;
CREATE TABLE `inforoom2_connectionRequests` (
	`id` INT(11) NOT NULL AUTO_INCREMENT,
	`serviceman` INT(11) NULL DEFAULT NULL,
	`comment` TEXT NULL,
	`client` INT(11) NOT NULL,
	`begintime` DATETIME NULL DEFAULT NULL,
	`endtime` DATETIME NULL DEFAULT NULL,
	PRIMARY KEY (`id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
