﻿CREATE TABLE `internet`.`Houses` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Street` VARCHAR(45) NOT NULL,
  `Number` INT(10) UNSIGNED NOT NULL,
  `Case` INT(10) UNSIGNED,
  `ApartmentCount` INT(10) UNSIGNED NOT NULL,
  `LastPassDate` DATETIME,
  `PassCount` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

CREATE TABLE `internet`.`Entrances` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `House` INT(10) UNSIGNED,
  `Number` INT(10) UNSIGNED NOT NULL,
  `Strut` TINYINT(1) UNSIGNED NOT NULL,
  `Cable` TINYINT(1) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `HouseKey` FOREIGN KEY `HouseKey` (`House`)
    REFERENCES `Houses` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

CREATE TABLE `internet`.`Apartments` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Entrance` INT(10) UNSIGNED NOT NULL,
  `LastInternet` VARCHAR(45) NOT NULL,
  `LastTV` VARCHAR(45) NOT NULL,
  `PresentInternet` VARCHAR(45) NOT NULL,
  `PresentTV` VARCHAR(45) NOT NULL,
  `Comment` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `EntranceKey` FOREIGN KEY `EntranceKey` (`Entrance`)
    REFERENCES `Entrances` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB;



DROP TEMPORARY TABLE IF EXISTS internet.Vostok;

CREATE TEMPORARY TABLE internet.Vostok (
ClientId INT unsigned);

INSERT
INTO    internet.Vostok

SELECT p.id FROM internet.physicalclients p
where p.Street like '%Восто%';

select * from internet.Vostok;

update internet.physicalclients pc, internet.Vostok v set
pc.Street = 'Юго-Восточный мкр.'
where pc.id = v.ClientId;



DROP TEMPORARY TABLE IF EXISTS internet.Sever;

CREATE TEMPORARY TABLE internet.Sever (
ClientId INT unsigned);

INSERT
INTO    internet.Sever

SELECT p.id FROM internet.physicalclients p
where p.Street like '%Север%';

select * from internet.Vostok;

update internet.physicalclients pc, internet.Sever s set
pc.Street = 'Северный мкр.'
where pc.id = s.ClientId;

update internet.physicalclients pc set
pc.Street = 'Аэродромная'
where pc.id = 59;

update internet.physicalclients pc set
pc.Street = 'Юго-Восточный мкр.'
where pc.id in (337,361);

insert into Internet.`Houses` (`street`, `number`, `case`)
SELECT p.street , p.house as number , p.casehouse FROM internet.physicalclients p
group by p.street , p.house , p.casehouse;

ALTER TABLE `internet`.`networkswitches` ADD COLUMN `PortCount` INT(10) UNSIGNED AFTER `Zone`;

ALTER TABLE `internet`.`Apartments` ADD COLUMN `Number` INT(10) UNSIGNED NOT NULL AFTER `Comment`;

ALTER TABLE `internet`.`Apartments` CHANGE COLUMN `Entrance` `House` INT(10) UNSIGNED NOT NULL,
 DROP INDEX `EntranceKey`,
 ADD INDEX `EntranceKey` USING BTREE(`House`),
 DROP FOREIGN KEY `EntranceKey`;

 ALTER TABLE `internet`.`Apartments` ADD CONSTRAINT `FK_Apartments_1` FOREIGN KEY `FK_Apartments_1` (`House`)
    REFERENCES `Houses` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`Entrances` ADD COLUMN `Switch` INT(10) UNSIGNED AFTER `Cable`,
 ADD CONSTRAINT `FK_Entrances_2` FOREIGN KEY `FK_Entrances_2` (`Switch`)
    REFERENCES `networkswitches` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`physicalclients` ADD COLUMN `HouseObj` INT(10) UNSIGNED AFTER `ConnectionPaid`;
ALTER TABLE `internet`.`physicalclients` ADD CONSTRAINT `FK_physicalclients_1` FOREIGN KEY `FK_physicalclients_1` (`HouseObj`)
    REFERENCES `Houses` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_physicalclients_2` FOREIGN KEY `FK_physicalclients_2` (`Tariff`)
    REFERENCES `Tariffs` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	update internet.physicalclients p
 join internet.Houses h on h.Street = p.Street and h.Number = p.House
set p.HouseObj = h.id;

ALTER TABLE `internet`.`Houses` CHANGE COLUMN `Case` `CaseHouse` INT(10) UNSIGNED DEFAULT NULL;


ALTER TABLE `internet`.`Apartments` MODIFY COLUMN `LastInternet` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `LastTV` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `PresentInternet` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `PresentTV` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `Comment` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `Number` INT(10) UNSIGNED NOT NULL;
 
 
 CREATE TABLE `internet`.`HouseAgents` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `DateOfBirth` DATETIME NOT NULL,
  `Telephone` VARCHAR(15) NOT NULL,
  `Adress` VARCHAR(45) NOT NULL,
  `PassportNumber` VARCHAR(8) NOT NULL,
  `PassportSeries` VARCHAR(6) NOT NULL,
  `PassportDate` DATETIME NOT NULL,
  `WhoGivePassport` VARCHAR(45) NOT NULL,
  `RegistrationAdress` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`HouseAgents` ADD COLUMN `Name` VARCHAR(45) NOT NULL AFTER `RegistrationAdress`,
 ADD COLUMN `Surname` VARCHAR(45) NOT NULL AFTER `Name`,
 ADD COLUMN `Patronymic` VARCHAR(45) NOT NULL AFTER `Surname`;

 
 CREATE TABLE `internet`.`BypassHouses` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `House` INT(10) UNSIGNED,
  `Agent` INT(10) UNSIGNED,
  `BypassDate` DATETIME,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_BypassHouses_1` FOREIGN KEY `FK_BypassHouses_1` (`Agent`)
    REFERENCES `HouseAgents` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_BypassHouses_2` FOREIGN KEY `FK_BypassHouses_2` (`House`)
    REFERENCES `Houses` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`Houses` ADD COLUMN `CompetitorCount` INT(10) UNSIGNED NOT NULL AFTER `PassCount`;

ALTER TABLE `internet`.`HouseAgents` MODIFY COLUMN `PassportDate` DATETIME DEFAULT NULL,
 MODIFY COLUMN `WhoGivePassport` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `RegistrationAdress` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL;


