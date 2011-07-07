DROP TEMPORARY TABLE IF EXISTS internet.obeshPlat;

CREATE TEMPORARY TABLE internet.obeshPlat (
ClientId INT unsigned,
spis decimal(10,2) NULL) engine=MEMORY ;

INSERT
INTO    internet.obeshPlat

select pc.Id, Sum(p.`Sum`) from internet.physicalClients pc
join internet.Clients c on pc.id = c.Physicalclient
join internet.Payments p on p.Client = c.id
where p.Agent = 29 and c.id <> 115
group by c.id;

select * from internet.obeshPlat;

update internet.physicalClients pc, internet.obeshPlat op set
pc.Balance = pc.Balance - op.spis
where pc.id = op.ClientId;


delete from Internet.payments
where agent = 29 and client <> 115;