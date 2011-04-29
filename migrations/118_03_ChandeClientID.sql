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


