DROP TEMPORARY TABLE IF EXISTS internet.StartNoBlockUpdate;

CREATE TEMPORARY TABLE internet.StartNoBlockUpdate (
NewStartNoBlock DATETIME,
clientid INT unsigned) engine=MEMORY ;

insert into internet.StartNoBlockUpdate (NewStartNoBlock, clientid)
SELECT max(logtime) as NewStartNoBlock, cl.id FROM `logs`.ClientInternetLogs C
join internet.clients cl on cl.id = c.clientid
where c.disabled = true and cl.startNoBlock < c.LogTime and cl.disabled = false
group by cl.id;

update internet.Clients c
join internet.StartNoBlockUpdate sn on sn.Clientid = c.id
set c.StartNoBlock = sn.NewStartNoBlock;