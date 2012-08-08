insert into Internet.ClientServices(Client, Service, ActivatedByUser)
select c.Id, s.Id, 1
from (Internet.PhysicalClients p, Internet.Services s)
join Internet.Clients c on c.PhysicalClient = p.Id
where s.Name like 'Internet';

insert into Internet.ClientServices(Client, Service, ActivatedByUser)
select c.Id, s.Id, 0
from (Internet.PhysicalClients p, Internet.Services s)
join Internet.Clients c on c.PhysicalClient = p.Id
where s.Name like 'IpTv';
