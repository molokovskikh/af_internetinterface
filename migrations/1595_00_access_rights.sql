USE internet;
ALTER TABLE `inforoom2_permissions`
	ALTER `Name` DROP DEFAULT;
ALTER TABLE `inforoom2_permissions`
	CHANGE COLUMN `Name` `Name` VARCHAR(255) NOT NULL AFTER `Id`,
	ADD COLUMN `Description` TEXT NOT NULL AFTER `Name`;

ALTER TABLE `inforoom2_roles`
	ALTER `Name` DROP DEFAULT;
ALTER TABLE `inforoom2_roles`
	CHANGE COLUMN `Name` `Name` VARCHAR(255) NOT NULL AFTER `Id`,
	ADD COLUMN `Description` TEXT NOT NULL AFTER `Name`;
