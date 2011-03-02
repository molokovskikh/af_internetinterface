update internet.PhysicalClients P set Balance = Balance - (590 / DateDiff('2011-04-01','2011-03-01'))
where P.id = 61;