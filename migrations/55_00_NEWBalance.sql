DROP TEMPORARY TABLE IF EXISTS internet.newbalance;

CREATE TEMPORARY TABLE internet.newbalance (
PClientId INT unsigned,
Balance DECIMAL(10,2) ) engine=MEMORY ;

INSERT
INTO    internet.newbalance

SELECT pc.id as PClientId,
pc.Balance - (
if (
DATEDIFF(CURDATE(),STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d')) > 0,
DATEDIFF(CURDATE(),STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d')),0) / 28 * t.Price
+
(DATEDIFF(CURDATE(), Min(i.LeaseBegin)) -
if (
DATEDIFF(CURDATE(),STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d')) > 0,
DATEDIFF(CURDATE(),STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d')),0)) / 31 * t.Price) AS Balance

FROM `logs`.InternetSessionsLogs I
join internet.ClientEndpoints CE on i.EndpointId = Ce.Id
join internet.Clients c on c.id = ce.Client
join internet.PhysicalClients pc on pc.id = c.PhisicalClient
join internet.tariffs t on t.id = pc.Tariff
#where i.LeaseBegin >= STR_TO_DATE('2011-02-01', '%Y-%m-%d')
group by i.EndpointId;

update internet.PhysicalClients pc, internet.newbalance nb set
pc.Balance = nb.Balance
where pc.id = nb.PClientId;