DROP TEMPORARY TABLE IF EXISTS internet.DebtBlockService;

CREATE TEMPORARY TABLE internet.DebtBlockService (
clientid INT unsigned) engine=MEMORY ;

insert into internet.DebtBlockService (ClientId)
SELECT Client FROM `logs`.ClientServiceInternetLogs C
where service = 3 and operation = 0
and BeginWorkDate > '2013-01-01'
group by client;

DROP TEMPORARY TABLE IF EXISTS internet.SaleUpdate;

CREATE TEMPORARY TABLE internet.SaleUpdate (
clientid INT unsigned,
newSale INT unsigned) engine=MEMORY ;

insert into internet.SaleUpdate (clientid, newSale)
SELECT c.clientid, max(sale) FROM `logs`.ClientInternetLogs C
join internet.DebtBlockService db on db.clientid = c.CLientid
where c.clientid in (
SELECT clientid FROM `logs`.ClientInternetLogs Cl
where cl.sale is not null
and cl.operatorName = 'zolotarev'
and logtime > '2013-08-01'
group by Cl.clientid)
group by c.clientid;

update internet.Clients c
join internet.SaleUpdate sn on sn.Clientid = c.id
set c.Sale = sn.newSale;