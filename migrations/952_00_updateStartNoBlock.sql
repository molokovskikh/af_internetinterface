DROP TEMPORARY TABLE IF EXISTS internet.DebtBlockService;

CREATE TEMPORARY TABLE internet.DebtBlockService (
clientid INT unsigned) engine=MEMORY ;

insert into internet.DebtBlockService (ClientId)
SELECT Client FROM `logs`.ClientServiceInternetLogs C
where service = 3 and operation = 0
and BeginWorkDate > '2013-01-01'
group by client;

DROP TEMPORARY TABLE IF EXISTS internet.StartTimeUpdate;

CREATE TEMPORARY TABLE internet.StartTimeUpdate (
clientid INT unsigned,
newStartDate DATETIME) engine=MEMORY ;

insert into internet.StartTimeUpdate (clientid)
SELECT c.clientid FROM `logs`.ClientInternetLogs C
join internet.DebtBlockService db on db.clientid = c.CLientid
join `logs`.ClientInternetLogs cl2 on cl2.logtime > '2013-08-01' and cl2.startNoBlock is not null and cl2.clientid = c.clientid
where c.clientid in (
SELECT clientid FROM `logs`.ClientInternetLogs Cl
where cl.sale is not null
and cl.operatorName = 'zolotarev'
and logtime > '2013-08-01'
group by Cl.clientid)
group by c.clientid;

update internet.StartTimeUpdate su
set newStartDate =  (
SELECT Cil.startNoBlock FROM `logs`.ClientInternetLogs Cil
where Cil.clientid = su.clientid and Cil.startNoBlock is not null
order by Cil.logtime desc
limit 1, 1);

update internet.Clients c
join internet.StartTimeUpdate sn on sn.Clientid = c.id
set c.startNoBlock = sn.newStartDate;