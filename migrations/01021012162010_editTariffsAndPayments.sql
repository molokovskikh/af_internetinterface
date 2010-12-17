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
