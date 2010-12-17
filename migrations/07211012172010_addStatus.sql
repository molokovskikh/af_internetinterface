CREATE TABLE `internet`.`Status` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


ALTER TABLE `internet`.`PhysicalClients` ADD COLUMN `Status` INT(10) UNSIGNED AFTER `Connected`,
 ADD CONSTRAINT `FK_PhysicalClients_4` FOREIGN KEY `FK_PhysicalClients_4` (`Status`)
    REFERENCES `Status` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

INSERT INTO `internet`.`Status` (`Name`) VALUES ('Активен');

INSERT INTO `internet`.`Status` (`Name`) VALUES ('Блокировка абонентом');

INSERT INTO `internet`.`Status` (`Name`) VALUES ('Принудительная блокировка');

ALTER TABLE `internet`.`Payments` MODIFY COLUMN `Agent` INT(10) UNSIGNED DEFAULT NULL;

UPDATE internet.Payments SET Agent = null
where Agent = 0;

ALTER TABLE `internet`.`Payments` ADD CONSTRAINT `FK_Payments_1` FOREIGN KEY `FK_Payments_1` (`Agent`)
    REFERENCES `Agents` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


