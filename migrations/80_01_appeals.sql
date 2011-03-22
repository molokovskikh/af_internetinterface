CREATE TABLE `internet`.`Appeals` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Appeal` TEXT NOT NULL,
  `Date` DATETIME NOT NULL,
  `Partner` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `Partner` FOREIGN KEY `Partner` (`Partner`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`Appeals` ADD COLUMN `PhysicalClient` INT(10) UNSIGNED AFTER `Partner`,
 ADD CONSTRAINT `PhysicalClient` FOREIGN KEY `PhysicalClient` (`PhysicalClient`)
    REFERENCES `PhysicalClients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
