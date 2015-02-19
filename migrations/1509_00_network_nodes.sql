user internet;
CREATE TABLE `inforoom2_network_nodes` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(250) NOT NULL,
	`Virtual` TINYINT(4) NOT NULL,
	`Description` TEXT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;

ALTER TABLE `inforoom2_switchaddress`
	CHANGE COLUMN `Switch` `NetworkNode` INT(11) NULL DEFAULT NULL AFTER `House`;

CREATE TABLE `inforoom2_twisted_pairs` (
	`id` INT(11) NOT NULL AUTO_INCREMENT,
	`pairCount` INT(11) NOT NULL,
	`networkNode` INT(11) NOT NULL,
	PRIMARY KEY (`id`)
)
ENGINE=InnoDB;

ALTER TABLE `networkswitches`
	ADD COLUMN `networkNode` INT NULL AFTER `Type`;