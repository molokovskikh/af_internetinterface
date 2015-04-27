use internet;
ALTER TABLE `inforoom2_house`
	ADD COLUMN `Region` INT UNSIGNED NULL DEFAULT NULL AFTER `Geomark`,
	ADD CONSTRAINT `HouseRegion` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`);
