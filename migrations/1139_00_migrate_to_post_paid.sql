update Internet.Orders
set IsActivated = 1
where BeginDate < curdate() - interval 1 day;

update Internet.Orders
set IsDeactivated = 1
where EndDate < curdate() - interval 1 day or Disabled = 1;

create temporary table Internet.for_delete engine=memory
select w.*
from Internet.Writeoff w
join Internet.OrderServices s on s.Id = w.Service
join Internet.Orders o on o.Id = s.OrderId
where WriteOffDate > '2014-02-01'
and w.Service is not null
and s.IsPeriodic = 1
and (o.EndDate is null or o.EndDate > curdate());

delete w from Internet.Writeoff w
join Internet.for_delete d on d.Id = w.Id;

update Internet.LawyerPerson l
join Internet.Clients c on c.LawyerPerson = l.Id
join Internet.for_delete d on d.Client = c.Id
set l.Balance = l.Balance + d.WriteOffSum;

drop temporary table Internet.for_delete;
