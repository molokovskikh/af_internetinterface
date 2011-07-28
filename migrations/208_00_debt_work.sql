ALTER TABLE `internet`.`Clients` ADD COLUMN `DebtWork` TINYINT(1) UNSIGNED NOT NULL AFTER `PostponedPayment`;


CREATE TABLE `internet`.`ClientServices` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Client` INT(10) UNSIGNED NOT NULL,
  `Service` INT(10) UNSIGNED NOT NULL,
  `BeginWorkDate` DATETIME,
  `EndWorkDate` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
