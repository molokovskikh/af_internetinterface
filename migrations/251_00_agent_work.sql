ALTER TABLE `internet`.`PaymentsForAgent` ADD COLUMN `Action` INT(10) UNSIGNED AFTER `RegistrationDate`,
 ADD CONSTRAINT `FK_PaymentsForAgent_2` FOREIGN KEY `FK_PaymentsForAgent_2` (`Action`)
    REFERENCES `AgentTariffs` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`requests` DROP COLUMN `Registered`;