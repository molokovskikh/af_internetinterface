update internet.Clients c
set c.PercentBalance = 0
where c.`Status` = 3;