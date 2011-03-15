DROP TEMPORARY TABLE IF EXISTS internet.updatePaidOn;

CREATE TEMPORARY TABLE internet.updatePaidOn (
PaymentId INT unsigned,
RegDate DateTime ) engine=MEMORY ;

INSERT
INTO    internet.updatePaidOn

SELECT P.id, pc.Regdate FROM internet.Payments P
join internet.PhysicalClients pc on pc.id = P.Client
where PaidOn = '2011-03-14 00:00:00';

select * from internet.updatePaidOn;

update internet.Payments p, internet.updatePaidOn ub set
p.PaidOn = ub.RegDate,
p.RecievedOn = ub.RegDate
where p.id = ub.PaymentId;