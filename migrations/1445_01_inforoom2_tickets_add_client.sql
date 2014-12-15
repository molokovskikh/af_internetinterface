ALTER TABLE internet.`inforoom2_tickets`
	ADD COLUMN `Client` INT(10) UNSIGNED NULL,
	ADD CONSTRAINT `FK_inforoom2_tickets_clients` FOREIGN KEY (`Client`) REFERENCES `clients` (`Id`);
	
	ALTER TABLE internet.`inforoom2_callmeback_tickets`
	ADD COLUMN `Client` INT(10) UNSIGNED NULL,
	ADD CONSTRAINT `FK_inforoom2_callbacktickets_clients` FOREIGN KEY (`Client`) REFERENCES `clients` (`Id`);
