DROP TEMPORARY TABLE IF EXISTS internet.deletePhysClients;

CREATE TEMPORARY TABLE internet.deletePhysClients (
PClientId INT unsigned) engine=MEMORY ;

INSERT
INTO    internet.deletePhysClients

SELECT pc.id FROM internet.Clients C
right join internet.PhysicalClients pc on pc.id = c.PhisicalClient
where c.id is null;

select * from internet.deletePhysClients;

delete from internet.PhysicalClients where id in
(select * from internet.deletePhysClients);

delete from internet.clients where id in
(147,149, 165, 137);

delete from internet.PhysicalClients where id in
(135,137, 179, 121);