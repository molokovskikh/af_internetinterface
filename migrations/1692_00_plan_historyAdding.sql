use internet;
CREATE TABLE `inforoom2_plan_history` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Client` INT(11) UNSIGNED NOT NULL DEFAULT '0',
	`PlanBefore` INT(11) UNSIGNED NOT NULL DEFAULT '0',
	`PlanAfter` INT(11) UNSIGNED NOT NULL DEFAULT '0',
	`DateOfChange` DATETIME NOT NULL DEFAULT '0000-00-00 00:00:00',
	`Price` DECIMAL(19,5) NOT NULL DEFAULT '0.00000',
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_plan_history_clients` (`Client`),
	INDEX `FK_inforoom2_plan_history_tariffs` (`PlanBefore`),
	INDEX `FK_inforoom2_plan_history_tariffs_2` (`PlanAfter`),
	CONSTRAINT `FK_inforoom2_plan_history_clients` FOREIGN KEY (`Client`) REFERENCES `clients` (`Id`),
	CONSTRAINT `FK_inforoom2_plan_history_tariffs` FOREIGN KEY (`PlanBefore`) REFERENCES `tariffs` (`Id`),
	CONSTRAINT `FK_inforoom2_plan_history_tariffs_2` FOREIGN KEY (`PlanAfter`) REFERENCES `tariffs` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;

ALTER TABLE `tariffs` ADD COLUMN `IsOnceOnly` TINYINT(1) NOT NULL DEFAULT '0' AFTER `Comments`;