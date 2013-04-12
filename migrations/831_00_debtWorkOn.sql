DROP TEMPORARY TABLE IF EXISTS internet.debService;

CREATE TEMPORARY TABLE internet.debService (
clientid INT unsigned ) engine=MEMORY ;

insert into internet.debService (clientid)
SELECT client FROM internet.ClientServices Cs
where service = 4;

insert into internet.ClientServices (Client, Service, BeginWorkDate, EndWorkDate, Activator, activated, diactivated)
select c.id, 4, now(), '2013-05-13', 1, 0, 0 from internet.clients c
where c.PhysicalClient is null and c.id not in (select clientid from internet.debService);