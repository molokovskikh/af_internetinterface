use internet;
ALTER TABLE `packagespeed`	ADD COLUMN `Confirmed` TINYINT NULL DEFAULT '0' AFTER `IsPhysic`;
UPDATE packagespeed SET Confirmed = 1;
