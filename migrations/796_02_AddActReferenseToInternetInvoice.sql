ALTER TABLE `internet`.`invoices` ADD COLUMN `Act` INTEGER UNSIGNED AFTER `Recipient`,
 ADD CONSTRAINT `FK_invoices_acts` FOREIGN KEY `FK_invoices_acts` (`Act`)
    REFERENCES `acts` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT;