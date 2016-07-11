 use internet;
 
 ALTER TABLE `clientendpoints`
	ADD COLUMN `IsEnabled` TINYINT(1) NULL DEFAULT NULL AFTER `Id`;
	
UPDATE `internet`.`clientendpoints` SET `IsEnabled`='1';