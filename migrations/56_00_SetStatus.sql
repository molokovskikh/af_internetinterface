update internet.PhysicalClients P set p.status = 7 where p.Balance < 0;

update internet.PhysicalClients P, internet.Clients C
 set p.status = 5 where p.Balance >= 0 and c.PhisicalClient = P.Id;