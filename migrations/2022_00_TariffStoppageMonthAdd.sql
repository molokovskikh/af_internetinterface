use internet;
ALTER TABLE `tariffs`
	ADD COLUMN `StoppageMonths` TINYINT(255) NULL DEFAULT NULL AFTER `IsOnceOnly`;