CREATE TABLE `Internet`.`Regions` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Region` VARCHAR(50),
  PRIMARY KEY(`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`houses` ADD COLUMN `RegionId` INTEGER UNSIGNED AFTER `CompetitorCount`,
 ADD CONSTRAINT `FK_houses_region` FOREIGN KEY `FK_houses_region` (`RegionId`)
    REFERENCES `regions` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT;


ALTER TABLE `internet`.`lawyerperson` ADD COLUMN `RegionId` INTEGER UNSIGNED AFTER `MailingAddress`,
 ADD CONSTRAINT `FK_lawyerperson_regios` FOREIGN KEY `FK_lawyerperson_regios` (`RegionId`)
    REFERENCES `regions` (`Id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT;

INSERT INTO internet.regions (Region) VALUES('Белгород');
INSERT INTO internet.regions (Region) VALUES('Борисоглебск');
INSERT INTO internet.regions (Region) VALUES('Воронеж');

	update internet.physicalclients p join internet.houses h on p.houseobj=h.id set h.RegionId = if(p.City='Белгород',(select Id from internet.regions r where r.region='Белгород'), (select Id from internet.regions r where r.region='Борисоглебск'));
	update internet.lawyerperson p set p.RegionId = if(p.actualadress like '%Белгород%',(select Id from internet.regions where region='Белгород'), if(p.actualadress like '%Борисоглебск%', (select Id from internet.regions where region='Борисоглебск'), (select Id from internet.regions where region='Воронеж')));