	use internet;
	ALTER TABLE `lawyerperson`	ADD COLUMN `_Address` INT(11) NULL DEFAULT NULL AFTER `PeriodEnd`;
	
	ALTER TABLE `lawyerperson`
	ADD CONSTRAINT `FK_lawyerperson_inforoom2_address` FOREIGN KEY (`_Address`) REFERENCES `inforoom2_address` (`Id`) ON UPDATE CASCADE ON DELETE RESTRICT;