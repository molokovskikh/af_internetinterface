use internet;

CREATE TABLE `inforoom2_connectedhouses` (
	`Id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Street` INT(11) NULL DEFAULT NULL,
	`Region` INT(10) UNSIGNED NULL DEFAULT NULL,
	`Number` VARCHAR(255) NOT NULL,
	`IsCustom` TINYINT(1) UNSIGNED ZEROFILL NOT NULL DEFAULT '0',
	`Comment` VARCHAR(255) NULL DEFAULT NULL,
	`Disabled` TINYINT(1) NOT NULL DEFAULT '0',
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_connectedhouses_inforoom2_street` (`Street`),
	INDEX `FK_inforoom2_connectedhouses_regions` (`Region`),
	CONSTRAINT `FK_inforoom2_connectedhouses_inforoom2_street` FOREIGN KEY (`Street`) REFERENCES `inforoom2_street` (`Id`) ON DELETE CASCADE,
	CONSTRAINT `FK_inforoom2_connectedhouses_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`) ON DELETE CASCADE
)