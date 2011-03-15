update internet.Clients c set
c.DebtDays = 0
where c.id<>15;

update internet.Clients c set
c.FirstLease = 0;