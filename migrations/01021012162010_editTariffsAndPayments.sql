ALTER TABLE `internet`.`Tariffs` ADD COLUMN `PackageId` INT(10) UNSIGNED NOT NULL AFTER `Price`;

ALTER TABLE `internet`.`Payments` ADD COLUMN `Agent` INT(10) UNSIGNED NOT NULL AFTER `Client`;

CREATE TABLE `internet`.`Agents` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Partner` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

INSERT INTO `internet`.`AccessCategories` (`Name`, `ReduceName`) VALUES ('Просмотр паспортных данных', 'VP');

ALTER TABLE `internet`.`AccessCategories` ADD COLUMN `Code` INT(10) UNSIGNED NOT NULL AFTER `ReduceName`;

UPDATE `internet`.`AccessCategories` SET `Code`='1' WHERE `Id`='1';

UPDATE `internet`.`AccessCategories` SET `Code`='2' WHERE `Id`='3';

UPDATE `internet`.`AccessCategories` SET `Code`='3' WHERE `Id`='5';

UPDATE `internet`.`AccessCategories` SET `Code`='4' WHERE `Id`='7';

UPDATE `internet`.`AccessCategories` SET `Code`='5' WHERE `Id`='9';

UPDATE `internet`.`AccessCategories` SET `Code`='6' WHERE `Id`='11';

UPDATE `internet`.`AccessCategories` SET `Code`='7' WHERE `Id`='13';

