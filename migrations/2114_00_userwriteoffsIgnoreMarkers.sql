USE internet;
ALTER TABLE `userwriteoffs`
	ADD COLUMN `ItemType` TINYINT UNSIGNED NOT NULL DEFAULT '0' AFTER `Registrator`,
	ADD COLUMN `ItemIgnore` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `ItemType`;