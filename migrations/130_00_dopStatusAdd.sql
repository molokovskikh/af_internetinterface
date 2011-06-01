CREATE TABLE `internet`.`AdditionalStatus` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Index` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
ALTER TABLE `internet`.`AdditionalStatus` MODIFY COLUMN `Index` INT(10) UNSIGNED NOT NULL;


CREATE TABLE `internet`.`StatusCorrelation` (
  `StatusId` INTEGER UNSIGNED NOT NULL,
  `AdditionalStatusId` INTEGER UNSIGNED NOT NULL,
  PRIMARY KEY (`StatusId`, `AdditionalStatusId`)
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`StatusCorrelation` MODIFY COLUMN `StatusId` INT(10) UNSIGNED NOT NULL,
 MODIFY COLUMN `AdditionalStatusId` INT(10) UNSIGNED NOT NULL,
 ADD CONSTRAINT `FK_StatusCorrelation_1` FOREIGN KEY `FK_StatusCorrelation_1` (`StatusId`)
    REFERENCES `Status` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_StatusCorrelation_2` FOREIGN KEY `FK_StatusCorrelation_2` (`AdditionalStatusId`)
    REFERENCES `AdditionalStatus` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
ALTER TABLE `internet`.`AdditionalStatus` CHANGE COLUMN `Index` `ShortName` VARCHAR(45) NOT NULL;

/*ALTER TABLE `internet`.`StatusCorrelation`
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
    ON UPDATE CASCADE;*/

	
	ALTER TABLE `internet`.`Clients` /*ADD COLUMN `AdditionalStatus` INT(10) UNSIGNED AFTER `Status`,*/
 ADD CONSTRAINT `AdStat` FOREIGN KEY `AdStat` (`AdditionalStatus`)
    REFERENCES `AdditionalStatus` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;

	
insert into internet.AdditionalStatus (Id, Name, ShortName) values
 (1, 'Отказ','Refused');

insert into internet.AdditionalStatus (Id, Name, ShortName) values
 (3, 'Недозвон','NotPhoned');

insert into internet.AdditionalStatus (Id, Name, ShortName) values
 (5, 'Назначить в граффик','AppointedToTheGraph');
 
 insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (1,1);
insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (3,1);
insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (1,3);
insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (3,3);
insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (1,5);
insert into internet.StatusCorrelation (StatusId, AdditionalStatusId) values
 (3,5);
 
 
 CREATE TABLE `internet`.`ConnectGraph` (
  `Id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `IntervalId` INT(10) UNSIGNED NOT NULL,
  `Client` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_ConnectGraph_1` FOREIGN KEY `FK_ConnectGraph_1` (`Client`)
    REFERENCES `clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`ConnectGraph` ADD COLUMN `Day` DATE NOT NULL AFTER `Client`;

ALTER TABLE `internet`.`ConnectGraph` MODIFY COLUMN `Day` DATE DEFAULT NULL;

ALTER TABLE `internet`.`ConnectGraph` ADD COLUMN `Brigad` INT(10) UNSIGNED AFTER `Day`,
 ADD CONSTRAINT `FK_ConnectGraph_2` FOREIGN KEY `FK_ConnectGraph_2` (`Brigad`)
    REFERENCES `connectbrigads` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

