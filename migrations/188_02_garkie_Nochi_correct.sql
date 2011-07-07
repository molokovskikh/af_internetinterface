DROP TEMPORARY TABLE IF EXISTS internet.garkieNochiCorrect;

CREATE TEMPORARY TABLE internet.garkieNochiCorrect (
ClientId INT unsigned,
spis decimal(10,2) NULL) engine=MEMORY ;

INSERT
INTO    internet.garkieNochiCorrect

select buf.Id, buf.`pSum`- buf.balance  - sum(w.WriteOffSum) from
(SELECT c.id, sum(p.Sum) as psum, pc.balance FROM internet.Clients C
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Payments p on p.client = c.id
where c.PhysicalClient is not null and c.id>=97 and pc.tariff = 10
group by c.id) as buf
left join internet.WriteOff w on w.Client = buf.id
group by buf.id;

select * from internet.garkieNochiCorrect;

select gn.*, pc.Balance from internet.physicalClients pc
join internet.Clients c on pc.id = c.PhysicalClient
join internet.garkieNochiCorrect gn on gn.ClientId = c.id;
  #set pc.Balance = pc.Balance - gn.spis;

update internet.physicalClients pc
join internet.Clients c on pc.id = c.PhysicalClient
join internet.garkieNochiCorrect gn on gn.ClientId = c.id  set
pc.Balance = pc.Balance + gn.spis;

select * from  internet.Clients c
join internet.PhysicalClients pc on c.PhysicalClient = pc.id and pc.tariff = 10;


update  internet.Clients c
join internet.PhysicalClients pc on c.PhysicalClient = pc.id and pc.tariff = 10
set c.debtdays = 0;