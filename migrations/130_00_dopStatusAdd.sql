CREATE TABLE `internet`.`AdditionalStatus` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Index` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
ALTER TABLE `internet`.`AdditionalStatus` MODIFY COLUMN `Index` INT(10) UNSIGNED NOT NULL;


CREATE TABLE `internet`.`StatusCorrelation` (
  `Status` INTEGER UNSIGNED NOT NULL,
  `AdditionalStatus` INTEGER UNSIGNED NOT NULL,
  PRIMARY KEY (`Status`, `AdditionalStatus`)
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`StatusCorrelation` MODIFY COLUMN `Status` INT(10) UNSIGNED NOT NULL,
 MODIFY COLUMN `AdditionalStatus` INT(10) UNSIGNED NOT NULL,
 ADD CONSTRAINT `FK_StatusCorrelation_1` FOREIGN KEY `FK_StatusCorrelation_1` (`Status`)
    REFERENCES `Status` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_StatusCorrelation_2` FOREIGN KEY `FK_StatusCorrelation_2` (`AdditionalStatus`)
    REFERENCES `AdditionalStatus` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
ALTER TABLE `internet`.`AdditionalStatus` CHANGE COLUMN `Index` `ShortName` VARCHAR(45) NOT NULL;

ALTER TABLE `internet`.`StatusCorrelation`
 DROP FOREIGN KEY `FK_StatusCorrelation_1`;

ALTER TABLE `internet`.`StatusCorrelation` CHANGE COLUMN `Status` `StatusId` INT(10) UNSIGNED NOT NULL,
 CHANGE COLUMN `AdditionalStatus` `AdditionalStatusId` INT(10) UNSIGNED NOT NULL,
 DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`StatusId`, `AdditionalStatusId`),
 DROP INDEX `FK_StatusCorrelation_2`,
 ADD INDEX `FK_StatusCorrelation_2` USING BTREE(`AdditionalStatusId`),
 ADD CONSTRAINT `FK_StatusCorrelation_1` FOREIGN KEY `FK_StatusCorrelation_1` (`StatusId`)
    REFERENCES `Status` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_StatusCorrelation_2` FOREIGN KEY `FK_StatusCorrelation_2` (`AdditionalStatusId`)
    REFERENCES `AdditionalStatus` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`Clients` ADD COLUMN `AdditionalStatus` INT(10) UNSIGNED AFTER `Status`,
 ADD CONSTRAINT `AdStat` FOREIGN KEY `AdStat` (`AdditionalStatus`)
    REFERENCES `AdditionalStatus` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
