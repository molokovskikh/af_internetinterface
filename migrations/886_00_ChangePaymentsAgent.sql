DROP temporary table if exists internet.ChangePayments;
CREATE TEMPORARY TABLE internet.ChangePayments (
PaymentId INT unsigned
)engine=MEMORY ;

insert into internet.ChangePayments
SELECT p.id FROM internet.payments p
join internet.clients c on c.id = p.client
join `logs`.PaymentInternetLogs Pl on pl.paymentid= p.id
where
 p.agent is null
 and c.LawyerPerson is not null
 and p.comment is null
 and operation = 0
 and p.RecievedOn > '2012-01-01'
 group by p.id;

 update internet.Payments p
set p.Agent = 81
where p.id in (select PaymentId from internet.ChangePayments);

ALTER TABLE `internet`.`payments` ADD CONSTRAINT `Agent` FOREIGN KEY `Agent` (`Agent`)
    REFERENCES `agents` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
