update Internet.Clients c
set c.DebtDays = 0;


update internet.Clients c
set c.RatedPeriodDate = '2011-04-14 00:00:00'
where c.id = 21;


update internet.Clients c
set c.RatedPeriodDate = '2011-04-17 00:00:00'
where c.id = 25;


update internet.Clients c
set c.RatedPeriodDate = '2011-04-17 00:00:00'
where c.id = 29;

update internet.Clients c
set c.RatedPeriodDate = '2011-04-19 00:00:00'
where c.id = 33;

update internet.InternetSettings set
NextBilligDate = '2011-04-20 22:00:00';