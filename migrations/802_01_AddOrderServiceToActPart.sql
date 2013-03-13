ALTER TABLE `internet`.`actparts` ADD COLUMN `OrderService` INTEGER UNSIGNED AFTER `Act`,
 ADD CONSTRAINT `FK_actparts_orderservice` FOREIGN KEY `FK_actparts_orderservice` (`OrderService`)
    REFERENCES `orderservices` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT;
