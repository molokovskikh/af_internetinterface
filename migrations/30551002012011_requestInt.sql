ALTER TABLE `internet`.`Requests` MODIFY COLUMN `House` VARCHAR(5) DEFAULT NULL,
 MODIFY COLUMN `CaseHouse` VARCHAR(5) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
 MODIFY COLUMN `Apartment` VARCHAR(5) DEFAULT NULL,
 MODIFY COLUMN `Entrance` VARCHAR(5) DEFAULT NULL,
 MODIFY COLUMN `Floor` VARCHAR(5) DEFAULT NULL;

 
 update internet.Requests R set R.Apartment = null where R.Apartment = "";
 
 update internet.Requests R set R.Entrance = null where R.Entrance = "";
 
 update internet.Requests R set R.Floor = null where R.Floor = "";
 
 update internet.Requests R set R.House = null where R.House = "";
 
 
 DROP TEMPORARY TABLE IF EXISTS Usersettings.ConnectedClients;

CREATE TEMPORARY TABLE Usersettings.ConnectedClients (
ClientId INT(10) unsigned,
id bigint unsigned,
INDEX (id)
)engine=MEMORY;

INSERT INTO Usersettings.ConnectedClients(ClientId)
SELECT pc.id FROM internet.ClientEndpoints C
join internet.Clients ic on ic.id = c.client
join internet.PhysicalClients pc on pc.Id = ic.PhisicalClient;

update internet.PhysicalClients pcs, Usersettings.ConnectedClients cc
 set pcs.Status = 3 where pcs.id = cc.ClientId;

update internet.PhysicalClients pcs
set pcs.Status = 1 where pcs.Status is null;