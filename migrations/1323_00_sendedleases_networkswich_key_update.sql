ALTER TABLE internet.sendedleases
	DROP FOREIGN KEY `Switch`,
	ADD CONSTRAINT `FK_internet_sendedleases_networkswitches` FOREIGN KEY (`Switch`) REFERENCES `networkswitches` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;