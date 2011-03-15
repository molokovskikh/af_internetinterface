update internet.Payments P set
P.BillingAccount = if (P.PaidOn < '2011-03-10 12:07:08', true, if (P.PaidOn > '2011-03-10 12:07:08' and P.Agent not in (1,13), true, false));

update internet.PhysicalClients pc set
pc.Balance = pc.Balance - 700
where pc.id = 81;


update internet.PhysicalClients pc set
pc.Balance = pc.Balance - 700
where pc.id = 85;


update internet.PhysicalClients pc set
pc.Balance = pc.Balance - 700
where pc.id = 87;
