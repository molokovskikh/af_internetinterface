ALTER TABLE internet.`inforoom2_plan`
	ALTER `Name` DROP DEFAULT;
ALTER TABLE internet.`inforoom2_plan`
	CHANGE COLUMN `Name` `Name` VARCHAR(255) NOT NULL AFTER `Id`,
	ADD COLUMN `Speed` INT(11) NOT NULL AFTER `Name`,
	ADD COLUMN `Price` DECIMAL(19,5) NOT NULL AFTER `Speed`,
	ADD COLUMN	`IsServicePlan` TINYINT(1) NULL DEFAULT NULL,
	ADD COLUMN	`IsArchived` TINYINT(1) NULL DEFAULT NULL;

	
CREATE TABLE internet.`inforoom2_plantransfer` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Price` DECIMAL(19,5) NOT NULL,
	`IsAvailableToSwitch` TINYINT(1) NULL DEFAULT NULL,
	`PlanFrom` INT(11) NOT NULL,
	`PlanTo` INT(11) NOT NULL,
	PRIMARY KEY (`Id`),
	INDEX `PlanFrom` (`PlanFrom`),
	INDEX `PlanTo` (`PlanTo`),
	CONSTRAINT `FKEE83DB87FFE5159` FOREIGN KEY (`PlanFrom`) REFERENCES internet.`inforoom2_plan` (`Id`),
	CONSTRAINT `FKEE83DB8E5D3D7E4` FOREIGN KEY (`PlanTo`) REFERENCES internet.`inforoom2_plan` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=10;

ALTER TABLE internet.`inforoom2_client` 
	ADD COLUMN `LastTimePlanChanged` DATETIME NULL AFTER `Email` ,
	ADD COLUMN `Balance` DECIMAL(19,5) NULL DEFAULT NULL,
	ADD COLUMN `Address` INT(11) NULL DEFAULT NULL,
	ADD COLUMN `Tariff` INT(11) NULL DEFAULT NULL,
	ADD COLUMN `PhoneNumber` VARCHAR(255) NOT NULL,
	ADD COLUMN `Name` VARCHAR(255) NOT NULL,
	ADD COLUMN	`Surname` VARCHAR(255) NOT NULL,
	ADD COLUMN	`Patronymic` VARCHAR(255) NOT NULL,
	ADD CONSTRAINT `FK656HGFFE6546HH` FOREIGN KEY (`Address`) REFERENCES internet.`inforoom2_address` (`Id`),
	ADD CONSTRAINT `FKFDF5456F454SJz` FOREIGN KEY (`Tariff`) REFERENCES internet.`inforoom2_plan` (`Id`);
	







