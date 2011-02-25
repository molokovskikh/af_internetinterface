DROP TEMPORARY TABLE IF EXISTS internet.newDate;

CREATE TEMPORARY TABLE internet.newDate (
ClientId INT unsigned,
RelatedDate DATETIME ) engine=MEMORY ;

INSERT
INTO    internet.newDate

SELECT c.id,
if (
DATEDIFF(CURDATE(),STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d')) > 0,
STR_TO_DATE(Concat('2011-02-',if (EXTRACT(DAY FROM i.LeaseBegin) < 28,EXTRACT(DAY FROM i.LeaseBegin),28)),'%Y-%m-%d'),
STR_TO_DATE(Concat('2011-01-',EXTRACT(DAY FROM i.LeaseBegin)),'%Y-%m-%d')) as RelatedDate

FROM `logs`.InternetSessionsLogs I
join internet.ClientEndpoints CE on i.EndpointId = Ce.Id
join internet.Clients c on c.id = ce.Client
join internet.PhysicalClients pc on pc.id = c.PhisicalClient
group by i.EndpointId;


update internet.Clients c, internet.newDate nd set
c.RatedPeriodDate = nd.RelatedDate
where c.id = nd.ClientId;