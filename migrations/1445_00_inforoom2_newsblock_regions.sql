ALTER TABLE internet.`inforoom2_newsblock`
	ADD COLUMN `Region` INT(10) UNSIGNED NULL AFTER `Priority`;
ALTER TABLE internet.`inforoom2_newsblock`
	ADD CONSTRAINT `FK_inforoom2_newsblock_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`);