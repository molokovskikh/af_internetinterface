ALTER TABLE `internet`.`LawyerPerson` ADD COLUMN `FullName` VARCHAR(255) AFTER `Client`,
 ADD COLUMN `ShortName` VARCHAR(45) AFTER `FullName`,
 ADD COLUMN `LawyerAdress` VARCHAR(255) AFTER `ShortName`,
 ADD COLUMN `ActualAdress` VARCHAR(255) AFTER `LawyerAdress`,
 ADD COLUMN `INN` VARCHAR(45) AFTER `ActualAdress`,
 ADD COLUMN `Email` VARCHAR(45) AFTER `INN`,
 ADD COLUMN `Telephone` VARCHAR(45) AFTER `Email`;

ALTER TABLE `internet`.`LawyerPerson` ADD COLUMN `Speed` INT(10) UNSIGNED NOT NULL AFTER `Telephone`;
