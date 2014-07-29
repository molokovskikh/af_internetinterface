create table Internet.RentableHardwares (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Cost NUMERIC(19,5) default 0  not null,
       Name VARCHAR(255),
       primary key (Id)
    );
