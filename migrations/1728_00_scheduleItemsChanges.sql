use internet;

ALTER TABLE `inforoom2_servicemenscheduleitems`	ALTER `Client` DROP DEFAULT;
ALTER TABLE `inforoom2_servicemenscheduleitems`	CHANGE COLUMN `Client` `Client` INT(11) NULL AFTER `Comment`;

UPDATE internet.inforoom2_servicemenscheduleitems as item  
SET item.Client = NULL 
WHERE item.ServiceRequest is Not NULL