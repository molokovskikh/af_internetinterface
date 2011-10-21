delete from internet.paymentforconnect;

ALTER TABLE `internet`.`paymentforconnect` CHANGE COLUMN `Summ` `Sum` VARCHAR(255) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 CHANGE COLUMN `ClientId` `EndPoint` INT(10) UNSIGNED DEFAULT NULL,
 CHANGE COLUMN `ManagerID` `Partner` INT(10) UNSIGNED DEFAULT NULL,
 ADD CONSTRAINT `FK_paymentforconnect_1` FOREIGN KEY `FK_paymentforconnect_1` (`EndPoint`)
    REFERENCES `clientendpoints` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_paymentforconnect_2` FOREIGN KEY `FK_paymentforconnect_2` (`Partner`)
    REFERENCES `partners` (`Id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT;
ALTER TABLE `internet`.`paymentforconnect` CHANGE COLUMN `PaymentDate` `RegDate` DATETIME DEFAULT NULL;
