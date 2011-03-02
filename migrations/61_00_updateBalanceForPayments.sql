DROP TEMPORARY TABLE IF EXISTS internet.newMinBalance;

CREATE TEMPORARY TABLE internet.newMinBalance (
PClientId INT unsigned,
Balance DECIMAL(10,2) ) engine=MEMORY ;

INSERT
INTO    internet.newMinBalance

SELECT pc.id, pc.Balance - t.Price / (28 + c.DebtDays) FROM Internet.Clients C
join internet.PhysicalClients pc on pc.id = c.PhisicalClient
join internet.Tariffs t on pc.Tariff = t.id
where pc.Balance < 0;


update internet.PhysicalClients pc, internet.newMinBalance nb set
pc.Balance = nb.Balance
where pc.id = nb.PClientId;




DROP TEMPORARY TABLE IF EXISTS internet.updateBalance;

CREATE TEMPORARY TABLE internet.updateBalance (
PClientId INT unsigned,
Sum DECIMAL(10,2) ) engine=MEMORY ;

INSERT
INTO    internet.updateBalance

SELECT Client, Sum(p.Sum) FROM internet.Payments P
join internet.PhysicalClients pc on pc.id = p.Client
where p.Agent in (1,13)
group by p.Client;

select * from internet.updateBalance;

update internet.PhysicalClients pc, internet.updateBalance ub set
pc.Balance = pc.Balance + ub.Sum
where pc.id = ub.PClientId;