alter table Internet.Clients add column Address VARCHAR(255);

update Internet.Clients c
join Internet.LawyerPerson l on l.Id = c.LawyerPerson
set c.Address = l.ActualAdress;

update Internet.Clients c
join Internet.PhysicalClients p on p.Id = c.PhysicalClient
set c.Address = concat('улица ', p.Street, ' дом ', p.House, if(p.CaseHouse is not null, concat(' корпус ', p.CaseHouse), ''), ' квартира ', p.Apartment, ' подъезд ', p.Entrance,' этаж ', p.Floor);
