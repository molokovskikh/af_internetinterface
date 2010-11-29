
    
alter table internet.PaymentsPhisicalClient  drop foreign key FK70AEB000BE8ECE31


    
alter table internet.PaymentsPhisicalClient  drop foreign key FK70AEB000E588D46A


    
alter table internet.PaymentForConnect  drop foreign key FK32DE3CAABE8ECE31


    
alter table internet.PaymentForConnect  drop foreign key FK32DE3CAAE588D46A


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07A2F141B3


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D0782A29C0E


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07C1F8E24E


    
alter table Internet.ConnectBrigads  drop foreign key FK6BF1015D33FA772A


    
alter table Internet.PartnerAccessSet  drop foreign key FKAF6779F890411CBB


    
alter table Internet.PartnerAccessSet  drop foreign key FKAF6779F8F8536E73


    
alter table Internet.RequestsConnection  drop foreign key FK79A337EE9CF6A269


    
alter table Internet.RequestsConnection  drop foreign key FK79A337EEE588D46A


    
alter table Internet.RequestsConnection  drop foreign key FK79A337EEBE8ECE31


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F883F7618E4


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F88C6A748B7


    drop table if exists Internet.NetworkSwitches

    drop table if exists internet.PaymentsPhisicalClient

    drop table if exists internet.Tariffs

    drop table if exists Internet.Clients

    drop table if exists internet.PaymentForConnect

    drop table if exists internet.AccessCategories

    drop table if exists internet.PhysicalClients

    drop table if exists Internet.ConnectBrigads

    drop table if exists Internet.PartnerAccessSet

    drop table if exists Internet.RequestsConnection

    drop table if exists internet.Partners

    drop table if exists Internet.ClientEndpoints

    create table Internet.NetworkSwitches (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Mac VARCHAR(255),
       Name VARCHAR(255),
       primary key (Id)
    )

    create table internet.PaymentsPhisicalClient (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PaymentDate DATETIME,
       Summ VARCHAR(255),
       ClientId INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Tariffs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Description VARCHAR(255),
       Price INTEGER,
       primary key (Id)
    )

    create table Internet.Clients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Disabled TINYINT(1),
       Name VARCHAR(255),
       Type INTEGER,
       PhisicalClient INTEGER,
       primary key (Id)
    )

    create table internet.PaymentForConnect (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PaymentDate DATETIME,
       Summ VARCHAR(255),
       ClientId INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.AccessCategories (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ReduceName VARCHAR(255),
       primary key (Id)
    )

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
    )

    create table Internet.ConnectBrigads (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Adress VARCHAR(255),
       BrigadCount INTEGER,
       Name VARCHAR(255),
       PartnerID INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.PartnerAccessSet (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Partner INTEGER UNSIGNED,
       AccessCat INTEGER,
       primary key (Id)
    )

    create table Internet.RequestsConnection (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RegDate DATETIME,
       CloseDemandDate DATETIME,
       BrigadNumber INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
       ClientID INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Partners (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Email VARCHAR(255),
       TelNum VARCHAR(255),
       Adress VARCHAR(255),
       RegDate DATETIME,
       Login VARCHAR(255),
       primary key (Id)
    )

    create table Internet.ClientEndpoints (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Disabled TINYINT(1),
       VLan INTEGER,
       Module INTEGER,
       Port INTEGER,
       Monitoring INTEGER,
       PackageId INTEGER,
       Client INTEGER UNSIGNED,
       Switch INTEGER UNSIGNED,
       primary key (Id)
    )

    alter table internet.PaymentsPhisicalClient 
        add index (ClientId), 
        add constraint FK70AEB000BE8ECE31 
        foreign key (ClientId) 
        references internet.PhysicalClients (Id)

    alter table internet.PaymentsPhisicalClient 
        add index (ManagerID), 
        add constraint FK70AEB000E588D46A 
        foreign key (ManagerID) 
        references internet.Partners (Id)

    alter table internet.PaymentForConnect 
        add index (ClientId), 
        add constraint FK32DE3CAABE8ECE31 
        foreign key (ClientId) 
        references internet.PhysicalClients (Id)

    alter table internet.PaymentForConnect 
        add index (ManagerID), 
        add constraint FK32DE3CAAE588D46A 
        foreign key (ManagerID) 
        references internet.Partners (Id)

    alter table internet.PhysicalClients 
        add index (Tariff), 
        add constraint FK7ACB2D07A2F141B3 
        foreign key (Tariff) 
        references internet.Tariffs (Id)

    alter table internet.PhysicalClients 
        add index (HasRegistered), 
        add constraint FK7ACB2D0782A29C0E 
        foreign key (HasRegistered) 
        references internet.Partners (Id)

    alter table internet.PhysicalClients 
        add index (HasConnected), 
        add constraint FK7ACB2D07C1F8E24E 
        foreign key (HasConnected) 
        references Internet.ConnectBrigads (Id)

    alter table Internet.ConnectBrigads 
        add index (PartnerID), 
        add constraint FK6BF1015D33FA772A 
        foreign key (PartnerID) 
        references internet.Partners (Id)

    alter table Internet.PartnerAccessSet 
        add index (Partner), 
        add constraint FKAF6779F890411CBB 
        foreign key (Partner) 
        references internet.Partners (Id)

    alter table Internet.PartnerAccessSet 
        add index (AccessCat), 
        add constraint FKAF6779F8F8536E73 
        foreign key (AccessCat) 
        references internet.AccessCategories (Id)

    alter table Internet.RequestsConnection 
        add index (BrigadNumber), 
        add constraint FK79A337EE9CF6A269 
        foreign key (BrigadNumber) 
        references Internet.ConnectBrigads (Id)

    alter table Internet.RequestsConnection 
        add index (ManagerID), 
        add constraint FK79A337EEE588D46A 
        foreign key (ManagerID) 
        references internet.Partners (Id)

    alter table Internet.RequestsConnection 
        add index (ClientID), 
        add constraint FK79A337EEBE8ECE31 
        foreign key (ClientID) 
        references internet.PhysicalClients (Id)

    alter table Internet.ClientEndpoints 
        add index (Client), 
        add constraint FKFCFB5F883F7618E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ClientEndpoints 
        add index (Switch), 
        add constraint FKFCFB5F88C6A748B7 
        foreign key (Switch) 
        references Internet.NetworkSwitches (Id)
