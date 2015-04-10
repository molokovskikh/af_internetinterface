use internet;
ALTER TABLE `physicalclients`
	ADD COLUMN `NewHouse` INT(11) NULL DEFAULT NULL AFTER `_Address`;
