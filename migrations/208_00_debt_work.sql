

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


ALTER TABLE `internet`.`ClientServices` ADD CONSTRAINT `FK_ClientServices_1` FOREIGN KEY `FK_ClientServices_1` (`Service`)
    REFERENCES `Services` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_ClientServices_2` FOREIGN KEY `FK_ClientServices_2` (`Activator`)
    REFERENCES `partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_ClientServices_3` FOREIGN KEY `FK_ClientServices_3` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
