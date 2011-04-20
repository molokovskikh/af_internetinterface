DROP TEMPORARY TABLE IF EXISTS internet.BeginDateClient;

CREATE TEMPORARY TABLE internet.BeginDateClient (
ClientId INT unsigned,
BeginDate DATETIME NULL) engine=MEMORY ;

INSERT
INTO    internet.BeginDateClient

SELECT c.id, Min(I.LeaseBegin) FROM internet.Clients C
join internet.ClientEndpoints CE on ce.Client = c.id
join `logs`.InternetSessionsLogs I on i.Endpointid = ce.id
where BeginWork is not null
group by c.id;

select * from internet.BeginDateClient;

update internet.Clients c, internet.BeginDateClient bd set
c.BeginWork = bd.BeginDate
where c.id = bd.ClientId;