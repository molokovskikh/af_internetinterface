USE internet;
ALTER TABLE `inforoom2_clientrentalhardware`
	ADD COLUMN `Used` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `GiveDate`,
	ADD COLUMN `CompleteSet` TINYINT(1) UNSIGNED NOT NULL DEFAULT '1' AFTER `Used`;
