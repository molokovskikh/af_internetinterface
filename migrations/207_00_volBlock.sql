ALTER TABLE `internet`.`Clients` ADD COLUMN `VoluntaryBlocking` TINYINT(1) UNSIGNED AFTER `PostponedPayment`;

ALTER TABLE `internet`.`Clients` ADD COLUMN `VoluntaryUnblockedDate` DATETIME AFTER `VoluntaryBlocking`;

ALTER TABLE `internet`.`status` ADD COLUMN `ShortName` VARCHAR(45) NOT NULL AFTER `Connected`;


ALTER TABLE `internet`.`Clients` CHANGE COLUMN `VoluntaryBlocking` `VoluntaryBlockingDate` DATETIME DEFAULT NULL;

ALTER TABLE `internet`.`Clients` CHANGE COLUMN `VoluntaryBlockingDate` `VoluntaryBlocking` TINYINT(1) UNSIGNED NOT NULL;



CREATE TABLE `internet`.`Services` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Price` DECIMAL NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
