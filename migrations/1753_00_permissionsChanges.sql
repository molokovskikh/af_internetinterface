use internet;
ALTER TABLE `inforoom2_permissions`
	ADD COLUMN `Hidden` TINYINT NOT NULL DEFAULT '0' AFTER `Description`;