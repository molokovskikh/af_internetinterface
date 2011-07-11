DROP TEMPORARY TABLE IF EXISTS internet.Correct;

CREATE TEMPORARY TABLE internet.Correct (
ClientId INT unsigned,
spis decimal(10,2) NULL) engine=MEMORY ;

INSERT
INTO    internet.Correct

select buf.Id, buf.`pSum`- buf.balance  - sum(w.WriteOffSum) from
(SELECT c.id, sum(p.Sum) as psum, pc.balance FROM internet.Clients C
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Payments p on p.client = c.id
where c.PhysicalClient is not null and c.id>=87 and pc.tariff != 10
group by c.id) as buf
left join internet.WriteOff w on w.Client = buf.id
group by buf.id;

select * from internet.Correct;

update internet.physicalClients pc
join internet.Clients c on pc.id = c.PhysicalClient
join internet.Correct cor on cor.ClientId = c.id  set
pc.Balance = pc.Balance + cor.spis;
