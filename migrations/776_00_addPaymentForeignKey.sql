ALTER TABLE `internet`.`payments` ADD CONSTRAINT `ClientKey` FOREIGN KEY `ClientKey` (`Client`)
    REFERENCES `clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `BankPaymentKey` FOREIGN KEY `BankPaymentKey` (`BankPayment`)
    REFERENCES `bankpayments` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
