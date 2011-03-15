ALTER TABLE `internet`.`ClientEndpoints`
 DROP FOREIGN KEY `FKBBAEFD19941C3747`;

ALTER TABLE `internet`.`ClientEndpoints` ADD CONSTRAINT `FKBBAEFD19941C3747` FOREIGN KEY `FKBBAEFD19941C3747` (`Client`)
    REFERENCES `clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 200
where pc.id = 23;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 1390
where pc.id = 9;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 600
where pc.id = 25;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 590
where pc.id = 53;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 590
where pc.id = 55;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 700
where pc.id = 47;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 700
where pc.id = 49;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 700
where pc.id = 51;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 1400
where pc.id = 59;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance - 700
where pc.id = 63;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance + 1290
where pc.id = 65;

update internet.PhysicalClients pc set
pc.Balance = pc.Balance - 700
where pc.id = 77;

delete from Internet.PhysicalClients where id = 79;