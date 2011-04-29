DROP TEMPORARY TABLE IF EXISTS internet.WriteOffNow;

CREATE TEMPORARY TABLE internet.WriteOffNow (
Client INT unsigned,
PhysicalClient INT unsigned,
writeoff decimal NULL) engine=MEMORY ;

INSERT
INTO    internet.WriteOffNow

SELECT c.id as Client, Pc.id as PhysicalClient,
if ((datediff(curdate(), c.BeginWork)-1)*(t.Price / 30) - Sum(w.WriteOffSum) < 0, 0, (datediff(curdate(), c.BeginWork)-1)*(t.Price / 30) - Sum(w.WriteOffSum)) as WriteOff
FROM internet.Clients C
join internet.writeOff w on w.Client = c.id
join internet.PhysicalClients pc on pc.id = c.PhysicalClient
join internet.Tariffs t on t.id = pc.Tariff
where c.RatedPeriodDate = '0001-01-01 00:00:00'
and c.physicalClient is not null
and c.beginwork is not null
group by c.id;

select * from internet.WriteOffNow;

update internet.Clients c, internet.WriteOffNow w set
c.RatedPeriodDate = beginwork
where c.id = w.Client;

update internet.PhysicalClients pc, internet.WriteOffNow w set
pc.Balance = pc.Balance - w.WriteOff
where pc.id = w.PhysicalClient;