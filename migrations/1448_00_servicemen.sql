use internet;
CREATE TABLE `inforoom2_servicemen` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Employee` INT(11) NOT NULL,
	PRIMARY KEY (`Id`)	
)
ENGINE=InnoDB;

ALTER TABLE `inforoom2_servicemen`
	ADD COLUMN `Region` INT(11) NOT NULL AFTER `Employee`;
	
ALTER TABLE `servicerequest`
	ADD COLUMN `_EndTime` DATETIME NULL DEFAULT NULL AFTER `Contact`;

ALTER TABLE `requests`
	ADD COLUMN `_EndTime` DATETIME NULL DEFAULT NULL AFTER `FriendThisClient`,
	ADD COLUMN `_BeginTime` DATETIME NULL DEFAULT NULL AFTER `_EndTime`,
	ADD COLUMN `_Serviceman` INT NULL DEFAULT NULL AFTER `_BeginTime`;