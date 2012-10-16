DROP TEMPORARY TABLE IF EXISTS internet.NoValidDebtWork;

CREATE TEMPORARY TABLE internet.NoValidDebtWork (
cs INT unsigned) engine=MEMORY ;

insert into internet.NovalidDebtWork
SELECT cs.id FROM internet.ClientServices Cs
join internet.Clients c on c.id = cs.client
join internet.PhysicalClients pc on pc.id = c.PhysicalClient
where cs.service = 1 and pc.balance > 0;

delete from internet.ClientServices
where id in (
select cs from internet.NoValidDebtWork);