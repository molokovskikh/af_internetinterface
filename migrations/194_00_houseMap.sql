insert into internet.usercategories (`Comment`, reductionName)
values ('Специалист по проходам','Agent');

insert into internet.accesscategories (`Name`, ReduceName)
values ('Доступ к карте дома','HMA');

insert into internet.accesscategories (`Name`, ReduceName)
values ('Доступ к интерфейсу агента','AIV');

CREATE TABLE `internet`.`ApartmentStatuses` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `ActivateDate` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`Apartments` ADD COLUMN `Status` INT(10) UNSIGNED AFTER `Number`,
 ADD CONSTRAINT `FK_Apartments_2` FOREIGN KEY `FK_Apartments_2` (`Status`)
    REFERENCES `ApartmentStatuses` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
ALTER TABLE `internet`.`ApartmentStatuses` MODIFY COLUMN `ActivateDate` INT(10) UNSIGNED DEFAULT NULL;


CREATE TABLE `internet`.`ApartmentHistory` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Apartment` INT(10) UNSIGNED,
  `ActionName` VARCHAR(255),
  `Agent` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_ApartmentHistory_1` FOREIGN KEY `FK_ApartmentHistory_1` (`Apartment`)
    REFERENCES `Apartments` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_ApartmentHistory_2` FOREIGN KEY `FK_ApartmentHistory_2` (`Agent`)
    REFERENCES `partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`ApartmentHistory` ADD COLUMN `ActionDate` DATETIME NOT NULL AFTER `Agent`;


ALTER TABLE `internet`.`requests` MODIFY COLUMN `Operator` INT(10) UNSIGNED DEFAULT NULL,
 ADD COLUMN `Registrator` INT(10) UNSIGNED AFTER `Operator`,
 ADD CONSTRAINT `FK_requests_1` FOREIGN KEY `FK_requests_1` (`Registrator`)
    REFERENCES `partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	ALTER TABLE `internet`.`requests` MODIFY COLUMN `Label` INT(10) UNSIGNED DEFAULT 0;
	
	ALTER TABLE `internet`.`ApartmentStatuses` ADD COLUMN `ShortName` VARCHAR(45) NOT NULL AFTER `ActivateDate`;

	
CREATE TABLE `internet`.`PaymentsForAgent` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Agent` INT(10) UNSIGNED NOT NULL,
  `Sum` DECIMAL(10,2) NOT NULL,
  `Comment` VARCHAR(255) NOT NULL,
  `RegistrationDate` DATETIME NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_PaymentsForAgent_1` FOREIGN KEY `FK_PaymentsForAgent_1` (`Agent`)
    REFERENCES `partners` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

CREATE TABLE `internet`.`AgentTariffs` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `ActionName` VARCHAR(45) NOT NULL,
  `Sum` DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`labels` ADD COLUMN `Deleted` TINYINT(1) UNSIGNED NOT NULL AFTER `Color`;

ALTER TABLE `internet`.`labels` ADD COLUMN `ShortComment` VARCHAR(45) AFTER `Deleted`;

ALTER TABLE `internet`.`physicalclients` ADD COLUMN `Request` INT(10) UNSIGNED AFTER `DateOfBirth`,
 ADD CONSTRAINT `FK_physicalclients_3` FOREIGN KEY `FK_physicalclients_3` (`Request`)
    REFERENCES `requests` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	
ALTER TABLE `internet`.`requests` ADD COLUMN `Registered` TINYINT(1) UNSIGNED NOT NULL AFTER `Registrator`;

