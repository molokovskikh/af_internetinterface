update internet.Clients c
set c.PercentBalance = 0.8
where c.`Status` = 3 and c.Disabled = false;