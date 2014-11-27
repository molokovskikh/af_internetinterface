ALTER TABLE internet.`physicalclients`
	ADD COLUMN `_Salt` VARCHAR(255) NULL AFTER `Additional`,
	ADD COLUMN `_Address` INT(11)  NULL AFTER `_Salt`,
	ADD COLUMN `_LastTimePlanChanged` DATE NULL AFTER `Tariff`,
	ADD COLUMN `_Email` VARCHAR(50) NOT NULL AFTER `ConnectSum`,
		ADD COLUMN `_PhoneNumber` VARCHAR(50) NOT NULL AFTER `_Email`,
	ADD CONSTRAINT `FK_physicalclients_inforoom2_address` FOREIGN KEY (`_Address`) REFERENCES internet.`inforoom2_address` (`Id`) ON UPDATE CASCADE;

	
	
	ALTER TABLE internet.`tariffs`
	ADD COLUMN `_Speed` INT NULL AFTER `iptv`,
	ADD COLUMN `_IsServicePlan` TINYINT NULL AFTER `_Speed`,
	ADD COLUMN `_IsArchived` TINYINT NULL AFTER `_IsServicePlan`;
	
ALTER TABLE internet.`inforoom2_plantransfer`
	ALTER `PlanFrom` DROP DEFAULT,
	ALTER `PlanTo` DROP DEFAULT;
	
ALTER TABLE internet.`inforoom2_plantransfer`
DROP FOREIGN KEY `FKEE83DB87FFE5159`,
DROP FOREIGN KEY `FKEE83DB8E5D3D7E4`;
	
ALTER TABLE internet.`inforoom2_plantransfer`
	CHANGE COLUMN `PlanFrom` `PlanFrom` INT(10) UNSIGNED NOT NULL AFTER `IsAvailableToSwitch`,
	CHANGE COLUMN `PlanTo` `PlanTo` INT(10) UNSIGNED NOT NULL AFTER `PlanFrom`,
	ADD CONSTRAINT `FKEE83DB87FFE5159` FOREIGN KEY (`PlanFrom`) REFERENCES internet.`tariffs` (`Id`),
	ADD CONSTRAINT `FKEE83DB8E5D3D7E4` FOREIGN KEY (`PlanTo`) REFERENCES internet.`tariffs` (`Id`);

	
ALTER TABLE internet.`inforoom2_region_plan`
	DROP FOREIGN KEY `FKE1E8D3C17E5929D3`,
	DROP FOREIGN KEY `FKE1E8D3C1AC7590E6`;
	
	ALTER TABLE internet.`inforoom2_street`
	DROP FOREIGN KEY `FK9D9AC8157E5929D3`;
	
	ALTER TABLE internet.`inforoom2_region`
	DROP FOREIGN KEY `FKBDB1C32BACFDF38`;
	
	DROP TABLE internet.inforoom2_region;
		
	
ALTER TABLE internet.`inforoom2_region_plan`
	CHANGE COLUMN `region` `region` INT(10) UNSIGNED NULL DEFAULT NULL FIRST,
	CHANGE COLUMN `plan` `plan` INT(10) UNSIGNED NOT NULL AFTER `region`;

	
ALTER TABLE internet.`inforoom2_region_plan`
	ADD CONSTRAINT `FKE1E8D3C17E5929D3` FOREIGN KEY (`region`) REFERENCES internet.`regions` (`Id`),
	ADD CONSTRAINT `FKE1E8D3C1AC7590E6` FOREIGN KEY (`plan`) REFERENCES internet.`tariffs` (`Id`);
	
	ALTER TABLE internet.`inforoom2_street`
	CHANGE COLUMN `Region` `Region` INT(10) UNSIGNED NULL DEFAULT NULL AFTER `District`;
	
ALTER TABLE internet.`inforoom2_street`
	ADD CONSTRAINT `FKE1E8H3C1AC7590E6` FOREIGN KEY (`Region`) REFERENCES internet.`regions` (`id`);
	
	ALTER TABLE internet.`regions`
	ADD COLUMN `_City` INT(11) NULL AFTER `IsExternalClientIdMandatory`,
	ADD CONSTRAINT `FK_regions_inforoom2_city` FOREIGN KEY (`_City`) REFERENCES internet.`inforoom2_city` (`Id`);
	
	ALTER TABLE internet.`services`
	ADD COLUMN `_Description` VARCHAR(255) NOT NULL AFTER `Configurable`,
	ADD COLUMN `_IsActivableFromWeb` TINYINT NOT NULL AFTER `_Description`;

	


