insert into internet.usercategories (`Comment`, reductionName)
values ('Специалист по проходам','Agent');

insert into internet.accesscategories (`Name`, ReduceName)
values ('Доступ к карте дома','HMA');

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
