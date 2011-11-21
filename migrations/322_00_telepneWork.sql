CREATE TABLE `internet`.`New Table` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Client` INT(10) UNSIGNED,
  `Contact` VARCHAR(45) NOT NULL,
  `Type` TINYINT(3) UNSIGNED NOT NULL,
  `Comment` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_New Table_1` FOREIGN KEY `FK_New Table_1` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`New Table` RENAME TO `internet`.`Contacts`;
ALTER TABLE `internet`.`Contacts` MODIFY COLUMN `Comment` VARCHAR(45) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL;
ALTER TABLE `internet`.`Contacts` ADD COLUMN `Date` DATETIME NOT NULL AFTER `Comment`,
 ADD COLUMN `Registrator` INT(10) UNSIGNED NOT NULL AFTER `Date`;
