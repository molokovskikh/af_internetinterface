ALTER TABLE `internet`.`bankpayments` DROP FOREIGN KEY `FK_BankPayments_1`;

update internet.bankpayments b
join internet.Clients c on c.LawyerPerson = b.PayerId
set b.PayerId = c.id;

ALTER TABLE `internet`.`lawyerperson` DROP COLUMN `Recipient`;

update internet.Clients c 
set c.Recipient = 7;