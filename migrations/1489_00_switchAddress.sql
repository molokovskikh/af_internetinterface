use internet;

ALTER TABLE `inforoom2_street`
	ADD COLUMN `Confirmed` TINYINT NOT NULL DEFAULT '0' AFTER `Region`;
ALTER TABLE `inforoom2_street`
	ADD COLUMN `Geomark` VARCHAR(255) NOT NULL AFTER `Confirmed`;
	
ALTER TABLE `inforoom2_house`
	ADD COLUMN `Confirmed` TINYINT NOT NULL DEFAULT '0' AFTER `Street`;	
ALTER TABLE `inforoom2_house`
	ADD COLUMN `Geomark` VARCHAR(255) NOT NULL AFTER `Confirmed`;
	
ALTER TABLE `inforoom2_switchaddress`
	DROP FOREIGN KEY `FKCB476ABD5D6871C7`;

CREATE TABLE `inforoom2_street_alias` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`street` INT NOT NULL,
	`name` VARCHAR(255) NOT NULL,
	`description` TEXT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
