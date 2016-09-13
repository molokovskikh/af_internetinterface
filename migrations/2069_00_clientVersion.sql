use internet;
ALTER TABLE `clients`
	ADD COLUMN `Version` INT(10) NOT NULL DEFAULT '0' AFTER `Comment`;
