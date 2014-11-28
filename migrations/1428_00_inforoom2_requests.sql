	ALTER TABLE internet.`requests`
	ADD COLUMN `_Address` INT(11)  NULL AFTER `Floor`,
	ADD CONSTRAINT `FK_REQUESTS_ADDRESS` FOREIGN KEY (`_Address`) REFERENCES internet.`inforoom2_address` (`Id`) ON UPDATE CASCADE;