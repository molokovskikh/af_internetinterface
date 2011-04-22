ALTER TABLE `internet`.`PhysicalClients` DROP COLUMN `Connected`;


ALTER TABLE `internet`.`Status` ADD COLUMN `Connected` TINYINT(1) UNSIGNED NOT NULL AFTER `Blocked`;


ALTER TABLE `internet`.`PhysicalClients` CHANGE COLUMN `PassportOutputDate` `PassportDate` DATETIME DEFAULT NULL;


ALTER TABLE `internet`.`PhysicalClients` ADD COLUMN `ConnectSum` DECIMAL(10,2) NOT NULL AFTER `AutoUnblocked`;


update internet.PhysicalClients pc, internet.PaymentForConnect pfc
set pc.ConnectSum = pfc.Summ
where pc.id = pfc.CLientId;

ALTER TABLE `internet`.`PhysicalClients` ADD COLUMN `WhoRegisteredName` VARCHAR(255) AFTER `ConnectSum`,
 ADD COLUMN `WhoConnectedName` VARCHAR(255) AFTER `WhoRegisteredName`;


ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `AutoUnblocked` TINYINT(1) UNSIGNED DEFAULT NULL,
 ADD COLUMN `ConnectionPaid` TINYINT(1) UNSIGNED AFTER `WhoConnectedName`;

ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `AutoUnblocked` TINYINT(1) UNSIGNED NOT NULL,
 MODIFY COLUMN `ConnectionPaid` TINYINT(1) UNSIGNED NOT NULL;


update internet.PhysicalClients pc, internet.Clients c
set pc.ConnectionPaid = true
where pc.id = c.PhysicalClient and c.BeginWork is not null;


DROP TEMPORARY TABLE IF EXISTS internet.nullPayments;

CREATE TEMPORARY TABLE internet.nullPayments (
Payment INT unsigned);

INSERT
INTO    internet.nullPayments

SELECT p.id FROM internet.Payments P
where Client not in (select pc.id from internet.PhysicalCLients pc);

delete from internet.Payments
where id in (select * from internet.nullPayments);


update internet.`Status` s
set s.Connected = true
where s.id in (5,7);