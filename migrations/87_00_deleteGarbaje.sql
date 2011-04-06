delete from internet.PhysicalClients
where id in (71, 73, 75, 89, 91, 93, 95, 27);

update internet.Clients C set c.DebtDays = 0
where C.Id = 14;