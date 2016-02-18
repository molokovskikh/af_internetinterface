use internet;
ALTER TABLE `payments`	ADD COLUMN `IsDuplicate` INT(1) NOT NULL DEFAULT '0' AFTER `Comment`;