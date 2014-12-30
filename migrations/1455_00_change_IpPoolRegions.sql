USE Internet;
ALTER TABLE `ippoolregions`
	ADD COLUMN `Description` VARCHAR(100) NULL DEFAULT NULL AFTER `Region`;
