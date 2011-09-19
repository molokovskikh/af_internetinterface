update internet.Requests r
join internet.PhysicalClients pc on pc.Request = r.Id
join internet.Clients c on c.PhysicalClient = pc.id
set r.PaidBonus = true
where c.BeginWork is not null;
