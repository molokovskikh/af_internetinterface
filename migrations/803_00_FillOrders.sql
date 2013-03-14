insert into internet.orders (Number, BeginDate, EndDate, EndPoint, ClientId, Disabled)
SELECT 1, c.RegDate, null, ce.Id, c.Id, 0  FROM internet.lawyerperson l join internet.clients c on c.lawyerperson=l.id and l.tariff is not null
left join internet.clientendpoints ce on ce.Client=c.Id;

INSERT INTO internet.orderservices (Description, Cost, IsPeriodic, OrderId)
SELECT 'Услуги доступа в интернет', l.Tariff, 1, o.Id  FROM internet.lawyerperson l join internet.clients c on c.lawyerperson=l.id and l.tariff is not null
join internet.orders o on o.ClientId=c.Id;