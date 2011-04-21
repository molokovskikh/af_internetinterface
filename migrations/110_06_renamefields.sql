ALTER TABLE `internet`.`PhysicalClients` CHANGE COLUMN `OutputDate` `PassportOutputDate` DATETIME DEFAULT NULL;


ALTER TABLE `internet`.`PhysicalClients` CHANGE COLUMN `HasRegistered` `WhoRegistered` INT(10) UNSIGNED DEFAULT NULL,
 CHANGE COLUMN `HasConnected` `WhoConnected` INT(10) UNSIGNED DEFAULT NULL;

