ALTER TABLE `internet`.`PaymentsForAgent` ADD COLUMN `Action` INT(10) UNSIGNED AFTER `RegistrationDate`,
 ADD CONSTRAINT `FK_PaymentsForAgent_2` FOREIGN KEY `FK_PaymentsForAgent_2` (`Action`)
    REFERENCES `AgentTariffs` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`requests` DROP COLUMN `Registered`;
	
	ALTER TABLE `internet`.`requests` ADD COLUMN `RegDate` DATETIME AFTER `Registrator`;
	
	ALTER TABLE `internet`.`requests` ADD COLUMN `VirtualBonus` DECIMAL(10,2) NOT NULL AFTER `RegDate`,
 ADD COLUMN `VirtualWriteOff` DECIMAL(10,2) NOT NULL AFTER `VirtualBonus`;
