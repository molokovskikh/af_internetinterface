ALTER TABLE `internet`.`Clients` ADD COLUMN `RatedPeriodDate` DATETIME AFTER `PhisicalClient`;

ALTER TABLE `internet`.`Clients` MODIFY COLUMN `Type` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `PhisicalClient` INT(10) UNSIGNED DEFAULT NULL,
 ADD CONSTRAINT `FK_Clients_1` FOREIGN KEY `FK_Clients_1` (`PhisicalClient`)
    REFERENCES `PhysicalClients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `Id` INT(10) UNSIGNED NOT NULL;

 ALTER TABLE `internet`.`PhysicalClients` ADD COLUMN `AutoUnblocked` TINYINT(1);
 
 ALTER TABLE `internet`.`Clients` ADD COLUMN `DebtDays` INT(3);
 
 ALTER TABLE `internet`.`Clients` MODIFY COLUMN `DebtDays` INT(3) UNSIGNED DEFAULT NULL;
 
 ALTER TABLE `internet`.`Clients` MODIFY COLUMN `DebtDays` INT(3) UNSIGNED NOT NULL;
 
 ALTER TABLE `internet`.`Clients` MODIFY COLUMN `Disabled` TINYINT(1) UNSIGNED DEFAULT NULL,
 ADD COLUMN `FirstLease` TINYINT(1) UNSIGNED NOT NULL;

 update `internet`.`PhysicalClients` set AutoUnblocked = true;
 
 ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `Balance` DECIMAL(10,2) DEFAULT NULL;
 
 ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `Balance` DECIMAL(10,2) NOT NULL;
 
update internet.PhysicalClients P set Balance = 0
where balance is null;

CREATE TABLE `Internet`.`InternetSettings` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `NextBillingDate` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


insert into
Internet.InternetSettings (id, NextBillingDate) value
 (1, CURDATE());