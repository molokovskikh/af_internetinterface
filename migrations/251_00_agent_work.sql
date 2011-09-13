ALTER TABLE `internet`.`PaymentsForAgent` ADD COLUMN `Action` INT(10) UNSIGNED AFTER `RegistrationDate`,
 ADD CONSTRAINT `FK_PaymentsForAgent_2` FOREIGN KEY `FK_PaymentsForAgent_2` (`Action`)
    REFERENCES `AgentTariffs` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`requests` DROP COLUMN `Registered`;
	
	ALTER TABLE `internet`.`requests` ADD COLUMN `RegDate` DATETIME AFTER `Registrator`;
	
	ALTER TABLE `internet`.`requests` ADD COLUMN `VirtualBonus` DECIMAL(10,2) NOT NULL AFTER `RegDate`,
 ADD COLUMN `VirtualWriteOff` DECIMAL(10,2) NOT NULL AFTER `VirtualBonus`;
ALTER TABLE `internet`.`requests` ADD COLUMN `PaidBonus` TINYINT(1) UNSIGNED NOT NULL AFTER `VirtualWriteOff`;


update internet.AgentTariffs a set
a.`Sum` = 50
where id = 1;

update internet.AgentTariffs a set
a.`Sum` = -200
where id = 3;

update internet.AgentTariffs a set
a.`Sum` = 50
where id = 5;

update internet.AgentTariffs a set
a.`Sum` = -200
where id = 7;

update internet.AgentTariffs a set
a.`Sum` = 50
where id = 9;

ALTER TABLE `internet`.`requests` MODIFY COLUMN `House` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `Apartment` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `Entrance` INT(10) UNSIGNED DEFAULT NULL,
 MODIFY COLUMN `Floor` INT(10) UNSIGNED DEFAULT NULL;