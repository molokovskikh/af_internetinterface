use internet;
ALTER TABLE `inforoom2_street`
	ADD COLUMN `Alias` VARCHAR(255) NULL DEFAULT NULL AFTER `Name`;