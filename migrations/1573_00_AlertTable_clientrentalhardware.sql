use internet;

ALTER TABLE `inforoom2_clientrentalhardware`
	ADD COLUMN `Employee` INT(11) UNSIGNED NULL DEFAULT NULL AFTER `Active`,
	ADD COLUMN `GiveDate` DATETIME NULL DEFAULT NULL AFTER `Employee`,
	ADD COLUMN `Comment` VARCHAR(255) NULL DEFAULT NULL AFTER `GiveDate`;
