use internet;
ALTER TABLE `servicerequest`
	CHANGE COLUMN `_EndTime` `EndTime` DATETIME NULL DEFAULT NULL AFTER `Contact`,
	ADD COLUMN `ServiceMan` INT NULL DEFAULT NULL AFTER `EndTime`,
	ADD COLUMN `BeginTime` DATETIME NULL DEFAULT NULL AFTER `BlockForRepair`;
