DROP TEMPORARY TABLE IF EXISTS internet.ratenull;

CREATE TEMPORARY TABLE internet.ratenull (
ClientId INT unsigned);

INSERT
INTO    internet.ratenull

SELECT c.id FROM internet.Clients C
where c.RatedPeriodDate = '0001-01-01 00:00:00'
and PhysicalClient is not null;

select * from internet.ratenull;

update internet.Clients c set
c.RatedPeriodDate = null
where c.id in (select * from internet.ratenull);