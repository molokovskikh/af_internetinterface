delete from internet.connectbrigads
where id <> 17;

update internet.Clients c set
c.WhoConnected = 17;

ALTER TABLE `internet`.`physicalclients` DROP COLUMN `RegDate`,
 DROP COLUMN `Status`,
 DROP COLUMN `WhoRegistered`,
 DROP COLUMN `WhoConnected`,
 DROP COLUMN `ConnectedDate`,
 DROP COLUMN `WhoRegisteredName`,
 DROP COLUMN `WhoConnectedName`,
 DROP FOREIGN KEY `FK_PhysicalClients_4`;
