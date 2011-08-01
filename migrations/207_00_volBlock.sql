ALTER TABLE `internet`.`status` ADD COLUMN `ShortName` VARCHAR(45) NOT NULL AFTER `Connected`;


ALTER TABLE `internet`.`Services` ADD COLUMN `BlockingAll` TINYINT(1) UNSIGNED NOT NULL AFTER `Price`;

ALTER TABLE `internet`.`Services` ADD COLUMN `HumanName` VARCHAR(45) NOT NULL AFTER `BlockingAll`;



CREATE TABLE `internet`.`Services` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Price` DECIMAL NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
