DROP TEMPORARY TABLE IF EXISTS internet.updateBalance;

CREATE TEMPORARY TABLE internet.updateBalance (
PClientId INT unsigned,
Sum DECIMAL(10,2) ) engine=MEMORY ;

INSERT
INTO    internet.updateBalance

SELECT Client, sum(p.Sum) FROM internet.Payments P
join internet.PhysicalClients pc on pc.id = p.Client
where p.Agent in (1,13) and p.Paidon > '2011-03-02 13:35:29'
group by p.Client;

select * from internet.updateBalance;

update internet.PhysicalClients pc, internet.updateBalance ub set
pc.Balance = pc.Balance + ub.Sum
where pc.id = ub.PClientId;