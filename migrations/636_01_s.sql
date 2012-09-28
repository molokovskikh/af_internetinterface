
    create table internet.DebtWorkInfos (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Sum NUMERIC(19,5) default 0  not null,
       Service INTEGER UNSIGNED,
       primary key (Id)
    );
alter table internet.DebtWorkInfos add index (Service), add constraint FK_internet_DebtWorkInfos_Service foreign key (Service) references Internet.ClientServices (Id);

ALTER TABLE `internet`.`debtworkinfos`
 DROP FOREIGN KEY `FK_internet_DebtWorkInfos_Service`;

ALTER TABLE `internet`.`debtworkinfos` ADD CONSTRAINT `FK_internet_DebtWorkInfos_Service` FOREIGN KEY `FK_internet_DebtWorkInfos_Service` (`Service`)
    REFERENCES `clientservices` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
