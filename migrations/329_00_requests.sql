ALTER TABLE `internet`.`Requests` ADD COLUMN `Archive` TINYINT(1) UNSIGNED NOT NULL AFTER `PaidBonus`,
 ADD COLUMN `Client` INT(10) UNSIGNED AFTER `Archive`,
 ADD CONSTRAINT `FK_Requests_6` FOREIGN KEY `FK_Requests_6` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
