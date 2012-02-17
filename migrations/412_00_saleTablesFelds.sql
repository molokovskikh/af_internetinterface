alter table internet.WriteOff add column Sale NUMERIC(19,5);
alter table internet.WriteOff add column Comment VARCHAR(255);

    create table internet.SaleSettings (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PeriodCount INTEGER,
       MinSale INTEGER,
       MaxSale INTEGER,
       SaleStep NUMERIC(19,5),
       primary key (Id)
    );
alter table Internet.Clients add column Sale NUMERIC(19,5) not null;
alter table Internet.Clients add column StartNoBlock DATETIME;
