use internet;
ALTER TABLE `inforoom2_address`
	CHANGE COLUMN `Entrance` `Entrance` VARCHAR(50) NULL DEFAULT NULL AFTER `Id`,
	CHANGE COLUMN `Apartment` `Apartment` VARCHAR(50) NULL DEFAULT NULL AFTER `Entrance`;
