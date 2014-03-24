insert into Internet.ClientServices(Client, Service, Activated, Endpoint)
select e.Client, (select Id from Internet.Services where Name = 'PinnedIp'), 1, e.Id
from Internet.ClientEndpoints e
where e.Ip is not null;
