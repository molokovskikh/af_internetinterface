update internet.Clients c set
c.id = c.id + 500;

update internet.Clients c set
c.id = c.PhysicalClient
where c.PhysicalClient is not null;


ALTER TABLE internet.Clients AUTO_INCREMENT=1000;


ALTER TABLE `internet`.`LawyerPerson` ADD COLUMN `WhoRegistered` INT(10) UNSIGNED AFTER `Speed`,
 ADD COLUMN `WhoRegisteredName` VARCHAR(45) NOT NULL AFTER `WhoRegistered`,
 ADD COLUMN `WhoConnected` INT(10) UNSIGNED AFTER `WhoRegisteredName`,
 ADD COLUMN `WhoConnectedName` VARCHAR(45) AFTER `WhoConnected`,
 ADD CONSTRAINT `FK_LawyerPerson_2` FOREIGN KEY `FK_LawyerPerson_2` (`WhoConnected`)
    REFERENCES `ConnectBrigads` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_LawyerPerson_3` FOREIGN KEY `FK_LawyerPerson_3` (`WhoRegistered`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`LawyerPerson` ADD COLUMN `Status` INT(10) UNSIGNED AFTER `WhoConnectedName`,
 ADD CONSTRAINT `FK_LawyerPerson_4` FOREIGN KEY `FK_LawyerPerson_4` (`Status`)
    REFERENCES `Status` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`LawyerPerson` DROP COLUMN `Client`,
 DROP FOREIGN KEY `FK_LawyerPerson_1`;

ALTER TABLE `internet`.`Clients` CHANGE COLUMN `SayBillingIsNewClient` `LawyerPerson` INT(10) UNSIGNED DEFAULT 

NULL,
 ADD CONSTRAINT `FK_Clients_2` FOREIGN KEY `FK_Clients_2` (`PhysicalClient`)
    REFERENCES `PhysicalClients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

ALTER TABLE `internet`.`LawyerPerson` MODIFY COLUMN `WhoRegisteredName` VARCHAR(45) CHARACTER SET cp1251 COLLATE 

cp1251_general_ci DEFAULT NULL;

update internet.Clients c set
c.LawyerPerson = null;


ALTER TABLE `internet`.`LawyerPerson` ADD COLUMN `RegDate` DATETIME AFTER `Status`;

ALTER TABLE `internet`.`LawyerPerson` MODIFY COLUMN `Speed` INT(10) UNSIGNED DEFAULT NULL;

ALTER TABLE `internet`.`LawyerPerson` ADD CONSTRAINT `Spped` FOREIGN KEY `Spped` (`Speed`)
    REFERENCES `PackageSpeed` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


insert into internet.LawyerPerson (ShortName)
(SELECT c.name FROM internet.Clients C
where physicalCLient is null);

update internet.LawyerPerson lp
join  internet.Clients c on c.Name = lp.shortname
join internet.ClientEndpoints cp on  cp.Client = c.id
left join internet.PackageSpeed p on cp.PackageId = p.PackageId set
lp.Whoregistered = 1,
lp.WhoRegisteredName = 'Золотарев А.А.',
lp.Status = 5,
lp.RegDate = curdate(),
lp.Speed = p.id
;

update internet.Clients c
join  internet.LawyerPerson lp on c.Name = lp.shortname
set c.LawyerPerson = lp.id;

ALTER TABLE `internet`.`Clients` ADD CONSTRAINT `Law` FOREIGN KEY `Law` (`LawyerPerson`)
    REFERENCES `LawyerPerson` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`Appeals` CHANGE COLUMN `PhysicalClient` `Client` INT(10) UNSIGNED DEFAULT NULL,
 DROP INDEX `PhysicalClient`,
 ADD INDEX `PhysicalClient` USING BTREE(`Client`),
 DROP FOREIGN KEY `PhysicalClient`,
 ADD CONSTRAINT `Clients` FOREIGN KEY `Clients` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


	
ALTER TABLE `internet`.`Clients` ADD COLUMN `RegDate` DATETIME AFTER `LawyerPerson`,
 ADD COLUMN `WhoRegistered` INT(10) UNSIGNED AFTER `RegDate`,
 ADD COLUMN `WhoRegisteredName` VARCHAR(45) AFTER `WhoRegistered`,
 ADD COLUMN `WhoConnected` INT(10) UNSIGNED AFTER `WhoRegisteredName`,
 ADD COLUMN `WhoConnectedName` VARCHAR(45) AFTER `WhoConnected`,
 ADD COLUMN `ConnectedDate` DATETIME AFTER `WhoConnectedName`,
 ADD COLUMN `AutoUnblocked` TINYINT(1) UNSIGNED NOT NULL AFTER `ConnectedDate`,
 ADD COLUMN `Status` INT(10) UNSIGNED AFTER `AutoUnblocked`,
 ADD CONSTRAINT `Part` FOREIGN KEY `Part` (`WhoRegistered`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `Connect` FOREIGN KEY `Connect` (`WhoConnected`)
    REFERENCES `ConnectBrigads` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `Status` FOREIGN KEY `Status` (`Status`)
    REFERENCES `Status` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

update internet.Clients C
left join internet.PhysicalClients pc on c.PhysicalClient = pc.id
left join internet.LawyerPerson lp on c.LawyerPerson = lp.id
set
c.RegDate = if (c.physicalClient is not null , pc.RegDate, lp.RegDate),
c.WhoRegistered = if (c.physicalClient is not null , pc.WhoRegistered, lp.WhoRegistered),
c.WhoRegisteredName = if (c.physicalClient is not null , pc.WhoRegisteredName, lp.WhoRegisteredName),
c.WhoConnected = if (c.physicalClient is not null , pc.WhoConnected, lp.WhoConnected),
c.WhoConnectedName = if (c.physicalClient is not null , pc.WhoConnectedName, lp.WhoConnectedName),
c.ConnectedDate = pc.ConnectedDate,
c.AutoUnblocked = true,
c.Status = if (c.physicalClient is not null , pc.Status, lp.Status);
