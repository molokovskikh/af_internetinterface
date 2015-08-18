use internet;
RENAME TABLE `inforoom2_connectionrequests` TO `inforoom2_ServicemenScheduleItems`;
ALTER TABLE `inforoom2_ServicemenScheduleItems`
	CHANGE COLUMN `serviceman` `Serviceman` INT(11) NULL DEFAULT NULL AFTER `id`,
	CHANGE COLUMN `comment` `Comment` TEXT NULL AFTER `Serviceman`,
	CHANGE COLUMN `client` `Client` INT(11) NOT NULL AFTER `Comment`,
	CHANGE COLUMN `begintime` `Begintime` DATETIME NULL DEFAULT NULL AFTER `Client`,
	CHANGE COLUMN `endtime` `Endtime` DATETIME NULL DEFAULT NULL AFTER `Begintime`,
	ADD COLUMN `ServiceRequest` INT(11) NULL DEFAULT NULL AFTER `Endtime`,
	ADD COLUMN `RequestType` INT(11) NULL DEFAULT NULL AFTER `ServiceRequest`,
	ADD COLUMN `Status` TINYINT NULL DEFAULT '0' AFTER `RequestType`; 
	
ALTER TABLE `inforoom2_logs`
	ADD COLUMN `ModelId` INT(11) UNSIGNED ZEROFILL NULL DEFAULT NULL AFTER `Employee`,
	ADD COLUMN `ModelClass` VARCHAR(50) NULL DEFAULT NULL AFTER `ModelId`;
