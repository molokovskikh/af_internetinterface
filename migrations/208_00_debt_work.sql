

CREATE TABLE `internet`.`ClientServices` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Client` INT(10) UNSIGNED NOT NULL,
  `Service` INT(10) UNSIGNED NOT NULL,
  `BeginWorkDate` DATETIME,
  `EndWorkDate` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
ALTER TABLE `internet`.`ClientServices` ADD COLUMN `Activated` TINYINT(1) UNSIGNED NOT NULL AFTER `EndWorkDate`;

ALTER TABLE `internet`.`ClientServices` ADD COLUMN `Activator` INT(10) UNSIGNED AFTER `Activated`;


ALTER TABLE `internet`.`ClientServices` ADD CONSTRAINT `FK_ClientServices_1` FOREIGN KEY `FK_ClientServices_1` (`Service`)
    REFERENCES `Services` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_ClientServices_2` FOREIGN KEY `FK_ClientServices_2` (`Activator`)
    REFERENCES `partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
 ADD CONSTRAINT `FK_ClientServices_3` FOREIGN KEY `FK_ClientServices_3` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`ClientServices` MODIFY COLUMN `Client` INT(10) UNSIGNED DEFAULT NULL,
 ADD COLUMN `Diactivated` TINYINT(1) UNSIGNED NOT NULL AFTER `Activator`;

 
 insert into internet.Services (Name, Price, BlockingAll, HumanName)
values ("DebtWork", 0, false, "Работа в долг"),
("VoluntaryBlockin", 0, true, "Добровольная блокировка");

insert into internet.ClientServices (Client, Service, BeginWorkDate, EndWorkDate, Activated, Diactivated)
SELECT c.id, 1,c.PostponedPayment, DATE_ADD(c.PostponedPayment, INTERVAL 1 DAY), if (c.Disabled, false, true), false  FROM internet.Clients C
where  c.PostponedPayment is not null;

update internet.`status` s
set ShortName = "BlockedAndNoConnected"
where s.id = 1;

update internet.`status` s
set ShortName = "BlockedAndConnected"
where s.id = 3;

update internet.`status` s
set ShortName = "Worked"
where s.id = 5;

update internet.`status` s
set ShortName = "NoWorked"
where s.id = 7;

insert into internet.`status` (id, name, blocked, connected, ShortName)
values (9, 'Добровольная блокировка', true, true, 'VoluntaryBlocking');