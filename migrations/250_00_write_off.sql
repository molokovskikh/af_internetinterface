CREATE TABLE `internet`.`UserWriteOff` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Sum` DECIMAL(10,2) NOT NULL,
  `Date` DATETIME NOT NULL,
  `Client` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_UserWriteOff_1` FOREIGN KEY `FK_UserWriteOff_1` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`UserWriteOff` RENAME TO `internet`.`UserWriteOffs`;

ALTER TABLE `internet`.`UserWriteOffs` ADD COLUMN `BillingAccount` TINYINT(1) UNSIGNED NOT NULL AFTER `Client`;

