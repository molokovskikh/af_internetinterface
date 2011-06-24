update Internet.clientendpoints c
left join internet.packagespeed p on p.PackageId = c.PackageId
join internet.Clients cs on cs.id = c.Client
join internet.PhysicalClients ps on ps.id = cs.PhysicalClient
set c.PackageId = 8
where ps.Tariff = 8;

update internet.Tariffs t set t.PackageId = 8
where t.Id = 8;

update internet.Tariffs t set t.Hidden = true
where t.Id = 1;

update Internet.clientendpoints c
left join internet.packagespeed p on p.PackageId = c.PackageId
join internet.Clients cs on cs.id = c.Client
join internet.PhysicalClients ps on ps.id = cs.PhysicalClient
join internet.Tariffs t on t.id = ps.tariff
set c.PackageId = t.PackageId
where c.PackageId = 0;