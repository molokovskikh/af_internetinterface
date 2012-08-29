
    create table Internet.TariffChangeRules (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Price NUMERIC(19,5) default 0  not null,
       FromTariff INTEGER UNSIGNED,
       ToTariff INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Internet.TariffChangeRules add index (FromTariff), add constraint FK_Internet_TariffChangeRules_FromTariff foreign key (FromTariff) references internet.Tariffs (Id) on delete cascade;
alter table Internet.TariffChangeRules add index (ToTariff), add constraint FK_Internet_TariffChangeRules_ToTariff foreign key (ToTariff) references internet.Tariffs (Id) on delete cascade;
