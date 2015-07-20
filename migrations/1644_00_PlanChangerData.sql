use internet;
CREATE TABLE `inforoom2_PlanChangerData` (
	`Id` INT(8) NOT NULL AUTO_INCREMENT,
	`TargetPlan` INT(8) NULL DEFAULT NULL,
	`CheapPlan` INT(8) NULL DEFAULT NULL,
	`FastPlan` INT(8) NULL DEFAULT NULL,
	`Timeout` INT(8) NULL DEFAULT NULL,
	`Text` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
ENGINE=InnoDB
; 
INSERT INTO `internet`.`services` (`Name`, `Price`, `BlockingAll`, `HumanName`, `InterfaceControl`, `_IsActivableFromWeb`) VALUES ('PlanChanger', 0, 0, 'PlanChanger', 0, 0);

ALTER TABLE `tariffs` CHANGE COLUMN `Published` `AvailableForNewClients` TINYINT(4) NULL DEFAULT NULL AFTER `Features`;
ALTER TABLE `tariffs` ADD COLUMN `AvailableForOldClients` TINYINT(4) NULL DEFAULT NULL AFTER `AvailableForNewClients`;
ALTER TABLE `tariffs` ADD COLUMN `Comments` VARCHAR(255) NULL DEFAULT NULL AFTER `Priority`;
