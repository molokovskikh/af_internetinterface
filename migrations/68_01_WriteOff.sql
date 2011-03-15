CREATE TABLE `internet`.`New Table` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `WriteOffSum` DECIMAL NOT NULL,
  `WriteOffDate` DATETIME NOT NULL,
  `Client` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `ClientRelation` FOREIGN KEY `ClientRelation` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;
