use internet;
ALTER TABLE `inforoom2_planchangerdata`
	CHANGE COLUMN `TargetPlan` `TargetPlan` INT(11) UNSIGNED NULL DEFAULT NULL AFTER `Id`,
	CHANGE COLUMN `CheapPlan` `CheapPlan` INT(11) UNSIGNED NULL DEFAULT NULL AFTER `TargetPlan`,
	CHANGE COLUMN `FastPlan` `FastPlan` INT(11) UNSIGNED NULL DEFAULT NULL AFTER `CheapPlan`;
ALTER TABLE `inforoom2_planchangerdata`
	ADD COLUMN `NotifyDays` INT(8) UNSIGNED NULL AFTER `Text`;
ALTER TABLE `inforoom2_planchangerdata`
	ADD CONSTRAINT `FK_inforoom2_planchangerdata_tariffs` FOREIGN KEY (`TargetPlan`) REFERENCES `tariffs` (`Id`),
	ADD CONSTRAINT `FK_inforoom2_planchangerdata_tariffs_2` FOREIGN KEY (`CheapPlan`) REFERENCES `tariffs` (`Id`),
	ADD CONSTRAINT `FK_inforoom2_planchangerdata_tariffs_3` FOREIGN KEY (`FastPlan`) REFERENCES `tariffs` (`Id`);
