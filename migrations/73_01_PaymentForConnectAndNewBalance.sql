update Internet.PaymentForConnect pc set
Summ = 700
where ClientId not in (3,55,61);

INSERT INTO Internet.Payments (RecievedOn, PaidOn , Sum, Client, Agent)
SELECT Curdate() as RecievedOn, Curdate() as PaidOn ,
if (p.id not in (63, 77,81),
if (p.id not in (3,55,61),t.Price + 700,t.Price),0) as Sum,
p.id as Client, 4 as Agent
FROM internet.PhysicalClients P
join internet.tariffs t on t.id = p.tariff;