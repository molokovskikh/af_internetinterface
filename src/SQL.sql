CREATE TABLE `internet`.`PhysicalClients` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Surname` VARCHAR(45) NOT NULL,
  `Patronymic` VARCHAR(45) NOT NULL,
  `City` VARCHAR(45) NOT NULL,
  `AdressConnect` VARCHAR(45) NOT NULL,
  `PassportSeries` VARCHAR(45) NOT NULL,
  `PassportNumber` VARCHAR(45) NOT NULL,
  `WhoGivePassport` VARCHAR(45) NOT NULL,
  `RegistrationAdress` VARCHAR(45) NOT NULL,
  `RegDate` DATETIME NOT NULL,
  `Tariff` INTEGER UNSIGNED NOT NULL,
  `Balance` DECIMAL(10,2) NOT NULL,
  `Login` VARCHAR(45) NOT NULL,
  `Password` VARCHAR(45) NOT NULL,
  `HasRegistered` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE internet.PhysicalClients ADD UNIQUE (Login)

CREATE TABLE `internet`.`PaymentsPhisicalClient` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `PaymentDate` DATETIME NOT NULL,
  `ClientID` INTEGER UNSIGNED NOT NULL,
  `Summ` DECIMAL NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`PaymentsPhisicalClient` 
  ADD CONSTRAINT `FK1`
  FOREIGN KEY (`ManagerID` )
  REFERENCES `accessright`.`Partners` (`Id` )
  ON DELETE SET NULL
  ON UPDATE CASCADE;

ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `Tariff` INT(10) UNSIGNED DEFAULT NULL,
 ADD CONSTRAINT `FK_PhysicalClients_1` FOREIGN KEY `FK_PhysicalClients_1` (`HasRegistered`)
    REFERENCES `accessright`.`Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	
ALTER TABLE `internet`.`PhysicalClients` ADD CONSTRAINT `FK_PhysicalClients_2` FOREIGN KEY `FK_PhysicalClients_2` (`Tariff`)
    REFERENCES `Tariffs` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

CREATE TABLE `accessright`.`AccessCategories` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Code` INTEGER UNSIGNED NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `accessright`.`Partners` ADD COLUMN `AcessSet` INTEGER NOT NULL AFTER `Login`;



CREATE TABLE `internet`.`ConnectBrigads` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `ResponsiblePerson` VARCHAR(45) NOT NULL,
  `Adress` VARCHAR(45) NOT NULL,
  `BrigadCount` INTEGER UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


CREATE TABLE `internet`.`RequestsConnection` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `BrigadNumber` INTEGER UNSIGNED NOT NULL,
  `ManagerID` INTEGER UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_RequestsConnection_1` FOREIGN KEY `FK_RequestsConnection_1` (`BrigadNumber`)
    REFERENCES `ConnectBrigads` (`Id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_RequestsConnection_2` FOREIGN KEY `FK_RequestsConnection_2` (`ManagerID`)
    REFERENCES `accessright`.`Partners` (`Id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`ConnectBrigads` ADD COLUMN `Name` VARCHAR(45) NOT NULL AFTER `BrigadCount`;


ALTER TABLE `internet`.`PaymentsPhisicalClient` MODIFY COLUMN `ClientID` INT(10) UNSIGNED DEFAULT NULL,
 ADD CONSTRAINT `FK_PaymentsPhisicalClient_2` FOREIGN KEY `FK_PaymentsPhisicalClient_2` (`ClientID`)
    REFERENCES `internet`.`PhysicalClients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


	ALTER TABLE `internet`.`ConnectBrigads` ADD COLUMN `PartnerID` INT(10) UNSIGNED AFTER `Name`,
 ADD CONSTRAINT `FK_ConnectBrigads_1` FOREIGN KEY `FK_ConnectBrigads_1` (`PartnerID`)
    REFERENCES `accessright`.`Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	
	ALTER TABLE `internet`.`ConnectBrigads` DROP COLUMN `ResponsiblePerson`;
ALTER TABLE `internet`.`RequestsConnection` ADD COLUMN `RegDate` DATETIME NOT NULL AFTER `ClientID`;


ALTER TABLE `internet`.`RequestsConnection` MODIFY COLUMN `BrigadNumber` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `ManagerID` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `ClientID` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `RegDate` DATETIME NOT NULL,
 ADD CONSTRAINT `FK_RequestsConnection_3` FOREIGN KEY `FK_RequestsConnection_3` (`ClientID`)
    REFERENCES `PhysicalClients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	ALTER TABLE `internet`.`RequestsConnection`
 DROP FOREIGN KEY `FK_RequestsConnection_1`;

ALTER TABLE `internet`.`RequestsConnection`
 DROP FOREIGN KEY `FK_RequestsConnection_2`;

ALTER TABLE `internet`.`RequestsConnection` ADD CONSTRAINT `FK_RequestsConnection_1` FOREIGN KEY `FK_RequestsConnection_1` (`BrigadNumber`)
    REFERENCES `connectbrigads` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_RequestsConnection_2` FOREIGN KEY `FK_RequestsConnection_2` (`ManagerID`)
    REFERENCES `accessright`.`partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	CREATE TABLE `internet`.`AccessCategories` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

CREATE TABLE `internet`.`PartnerAccessSet` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Partner` INTEGER UNSIGNED,
  `AccessCategories` INTEGER UNSIGNED,
  PRIMARY KEY (`Id`),
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`PartnerAccessSet` ADD CONSTRAINT `FK_PartnerAccessSet_1` FOREIGN KEY `FK_PartnerAccessSet_1` (`AccessCat`)
    REFERENCES `AccessCategories` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	
ALTER TABLE `internet`.`PartnerAccessSet` ADD CONSTRAINT `FK_PartnerAccessSet_2` FOREIGN KEY `FK_PartnerAccessSet_2` (`Partner`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`AccessCategories` ADD COLUMN `ReduceName` VARCHAR(5) NOT NULL AFTER `Name`;

ALTER TABLE `internet`.`PaymentsPhisicalClient` ADD CONSTRAINT `FK_PaymentsPhisicalClient_2` FOREIGN KEY `FK_PaymentsPhisicalClient_2` (`ManagerID`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	
ALTER TABLE `internet`.`Clients` ADD COLUMN `Type` ENUM('Phisical','Legal') NOT NULL AFTER `Name`,
 ADD COLUMN `PhisicalClient` INT(10) UNSIGNED NOT NULL AFTER `Type`;

 ALTER TABLE `internet`.`RequestsConnection` ADD COLUMN `CloseDemandDate` DATETIME NOT NULL AFTER `RegDate`;

 
 ALTER TABLE `internet`.`Clients` CHANGE COLUMN `Client` `PhisicalClient` INT(10) UNSIGNED NOT NULL;

