DROP TEMPORARY TABLE IF EXISTS internet.ServiceRequestsInsert;

CREATE TEMPORARY TABLE internet.ServiceRequestsInsert (
serviceId INT unsigned,
clientid INT unsigned,
Sum decimal unsigned,
comment varchar(255) ) engine=MEMORY ;

insert into internet.ServiceRequestsInsert (serviceId, clientid, Sum, comment)
SELECT s.Id, s.client, s.sum, Concat('Оказание дополнительных услуг, заявка №', s.id) FROM internet.ServiceRequest S
left join internet.UserWriteOffs w on w.Client = s.Client and w.Comment like 'Оказание дополнительных услуг, заявка %'
join internet.Clients c on s.client = c.id
join internet.PhysicalClients pc on pc.Id = c.PhysicalClient
where s.Sum is not null and s.ClosedDate >= '2013-07-01' and w.id is null and (pc.Balance - s.sum > 0 or s.id = 2231);

insert into internet.UserWriteOffs (Sum, Date, Client, Comment, Registrator)
select sr.sum, curdate(), sr.ClientId, sr.Comment, 1 from  internet.ServiceRequestsInsert sr
;