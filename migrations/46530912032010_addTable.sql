alter table Internet.Requests add column Label INTEGER UNSIGNED;

    create table Internet.ConnectBrigads (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Adress VARCHAR(255),
       BrigadCount INTEGER,
       Name VARCHAR(255),
       PartnerID INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Internet.Clients add column Type INTEGER;
alter table Internet.Clients add column PhisicalClient INTEGER;

    create table internet.AccessCategories (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ReduceName VARCHAR(255),
       primary key (Id)
    );

    create table Internet.PartnerAccessSet (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Partner INTEGER UNSIGNED,
       AccessCat INTEGER,
       primary key (Id)
    );
