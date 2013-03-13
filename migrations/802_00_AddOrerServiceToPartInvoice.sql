ALTER TABLE `internet`.`invoiceparts` ADD COLUMN `OrderService` INTEGER UNSIGNED AFTER `Invoice`,
 ADD CONSTRAINT `FK_invoiceparts_orderservice` FOREIGN KEY `FK_invoiceparts_orderservice` (`OrderService`)
    REFERENCES `orderservices` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT;
