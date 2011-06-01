DROP TEMPORARY TABLE IF EXISTS internet.garkieNochi;

CREATE TEMPORARY TABLE internet.garkieNochi (
ClientId INT unsigned,
plus DECIMAL(10,2)) engine=MEMORY ;

INSERT
INTO    internet.garkieNochi

SELECT pc.id, (520/31*datediff(curdate(), c.beginWork) - 300/31*datediff(curdate(), c.beginWork)) as plus FROM Internet.clients c
join internet.PhysicalClients pc on c.PhysicalClient = pc.id
join internet.Tariffs t on pc.Tariff = t.id
where t.id = 10;

select * from internet.garkieNochi;

update internet.physicalclients pc, internet.garkieNochi g set
pc.balance = pc.balance + g.plus
where pc.id = g.ClientId;