USE internet;
ALTER TABLE `inforoom2_tvchannels`
	ADD COLUMN `Enabled` TINYINT NOT NULL AFTER `Port`,
	ADD COLUMN `Priority` INT NOT NULL DEFAULT '0' AFTER `Enabled`;
