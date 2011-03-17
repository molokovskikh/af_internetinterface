delete from Internet.WriteOff;


insert into Internet.WriteOff (WriteOffSum, WriteOffDate,Client)
select pc.summ as WriteOffSum, Curdate() as WriteOffDate, c.Id as Client
from Internet.PaymentForConnect pc
join internet.Clients c on c.PhisicalClient = pc.ClientId
where pc.Summ > 0;