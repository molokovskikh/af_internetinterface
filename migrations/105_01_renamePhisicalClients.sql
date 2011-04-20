ALTER TABLE `internet`.`Clients` CHANGE COLUMN `PhisicalClient` `PhysicalClient` INT(10) UNSIGNED DEFAULT NULL,
 DROP INDEX `FK_Clients_1`,
 ADD INDEX `FK_Clients_1` USING BTREE(`PhysicalClient`);
