

CREATE TABLE `internet`.`ClientServices` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Client` INT(10) UNSIGNED NOT NULL,
  `Service` INT(10) UNSIGNED NOT NULL,
  `BeginWorkDate` DATETIME,
  `EndWorkDate` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
ALTER TABLE `internet`.`ClientServices` ADD COLUMN `Activated` TINYINT(1) UNSIGNED NOT NULL AFTER `EndWorkDate`;

ALTER TABLE `internet`.`ClientServices` ADD COLUMN `Activator` INT(10) UNSIGNED AFTER `Activated`;
