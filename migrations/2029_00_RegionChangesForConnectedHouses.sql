use internet;

ALTER TABLE `regions`
	ADD COLUMN `GenerateConnectedHouses` TINYINT(1) NOT NULL DEFAULT '0' AFTER `Parent`;