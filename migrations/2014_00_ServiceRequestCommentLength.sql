use internet;
ALTER TABLE `serviceiterations`
	CHANGE COLUMN `Description` `Description` TEXT NULL DEFAULT NULL AFTER `Id`;