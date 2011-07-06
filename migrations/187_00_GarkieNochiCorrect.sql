update  internet.writeoff w
join internet.Clients c on c.id = w.Client
join internet.PhysicalClients pc on pc.Id = c.PhysicalClient
join internet.Tariffs t on t.id = pc.Tariff
set w.WriteOffSum = 9.68
where t.id = 10 and WriteOffSum > 16;