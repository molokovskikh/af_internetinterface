
    create table internet.Partners (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Email VARCHAR(255),
       TelNum VARCHAR(255),
       Adress VARCHAR(255),
       RegDate DATETIME,
       Login VARCHAR(255),
       primary key (Id)
    );

    create table Internet.RequestsConnection (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RegDate DATETIME,
       CloseDemandDate DATETIME,
       BrigadNumber INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       ClientID INTEGER UNSIGNED,
       primary key (Id)
    );

    create table internet.PaymentsPhisicalClient (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PaymentDate DATETIME,
       Summ VARCHAR(255),
       ClientId INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       primary key (Id)
    );

    create table internet.PhysicalClients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Surname VARCHAR(255),
       Patronymic VARCHAR(255),
       City VARCHAR(255),
       Street VARCHAR(255),
       House VARCHAR(255),
       CaseHouse VARCHAR(255),
       Apartment VARCHAR(255),
       Entrance VARCHAR(255),
       Floor VARCHAR(255),
       PhoneNumber VARCHAR(255),
       HomePhoneNumber VARCHAR(255),
       WhenceAbout VARCHAR(255),
       PassportSeries VARCHAR(255),
       PassportNumber VARCHAR(255),
       OutputDate VARCHAR(255),
       WhoGivePassport VARCHAR(255),
       RegistrationAdress VARCHAR(255),
       RegDate DATETIME,
       Balance VARCHAR(255),
       Login VARCHAR(255),
       Password VARCHAR(255),
       Connected TINYINT(1),
       Tariff INTEGER UNSIGNED,
       HasRegistered INTEGER UNSIGNED,
       HasConnected INTEGER UNSIGNED,
       primary key (Id)
    );

    create table internet.PaymentForConnect (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PaymentDate DATETIME,
       Summ VARCHAR(255),
       ClientId INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       primary key (Id)
    );

    create table Internet.Labels (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Color VARCHAR(255),
       primary key (Id)
    );
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
