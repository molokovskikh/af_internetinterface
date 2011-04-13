CREATE TABLE `internet`.`LawyerPerson` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Tariff` DECIMAL NOT NULL,
  `Balance` DECIMAL NOT NULL,
  `Client` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_LawyerPerson_1` FOREIGN KEY `FK_LawyerPerson_1` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;