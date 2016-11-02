use internet;
ALTER TABLE `inforoom2_banner`
	ADD COLUMN `Target` TINYINT NOT NULL DEFAULT '0' AFTER `Enabled`;
ALTER TABLE `inforoom2_banner`
	ADD COLUMN `Name` TEXT NULL DEFAULT NULL AFTER `ImagePath`;