USE internet;
CREATE TABLE `tvrequests` (
	`id` INT(11) NOT NULL AUTO_INCREMENT,
	`partner` INT(11) NOT NULL,
	`date` DATETIME NOT NULL,
	`contact` INT(11) NULL DEFAULT NULL,
	`additionalcontact` VARCHAR(100) NULL DEFAULT NULL,
	`comment` TEXT NULL,
	`hdmi` TINYINT(4) NOT NULL,
	`client` INT(11) NOT NULL,
	PRIMARY KEY (`id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=14;

