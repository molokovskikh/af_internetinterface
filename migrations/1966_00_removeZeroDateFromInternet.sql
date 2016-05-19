use internet;
ALTER TABLE `lawyerperson`
	CHANGE COLUMN `PeriodEnd` `PeriodEnd` DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00' AFTER `RegionId`;