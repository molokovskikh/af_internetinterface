
    
alter table Internet.RequestsConnection  drop foreign key FK6A05CD54B5EBDA3A


    
alter table Internet.RequestsConnection  drop foreign key FK6A05CD54EBEC559F


    
alter table Internet.RequestsConnection  drop foreign key FK6A05CD54C7F2DBAC


    
alter table internet.PaymentsPhisicalClient  drop foreign key FK82F42559C7F2DBAC


    
alter table internet.PaymentsPhisicalClient  drop foreign key FK82F42559EBEC559F


    
alter table Internet.ClientEndpoints  drop foreign key FKBBAEFD1960C6D208


    
alter table Internet.ClientEndpoints  drop foreign key FKBBAEFD197F59B68D


    
alter table internet.PhysicalClients  drop foreign key FKDE111BCFE6F8BB1B


    
alter table internet.PhysicalClients  drop foreign key FKDE111BCF10E504C1


    
alter table internet.PhysicalClients  drop foreign key FKDE111BCF1DF06EC1


    
alter table Internet.ConnectBrigads  drop foreign key FK14B0BE5EF29DAF50


    
alter table Internet.PartnerAccessSet  drop foreign key FKFB2A341A486245DF


    
alter table Internet.PartnerAccessSet  drop foreign key FKFB2A341A4370E58F


    drop table if exists internet.AccessCategories

    drop table if exists Internet.RequestsConnection

    drop table if exists internet.PaymentsPhisicalClient

    drop table if exists Internet.Clients

    drop table if exists Internet.ClientEndpoints

    drop table if exists internet.PhysicalClients

    drop table if exists Internet.ConnectBrigads

    drop table if exists Internet.NetworkSwitches

    drop table if exists internet.Tariffs

    drop table if exists internet.Partners

    drop table if exists Internet.PartnerAccessSet

    create table internet.AccessCategories (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ReduceName VARCHAR(255),
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

    create table internet.PaymentsPhisicalClient (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PaymentDate DATETIME,
       Summ VARCHAR(255),
       ClientId INTEGER UNSIGNED,
       ManagerID INTEGER UNSIGNED,
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

    create table internet.PhysicalClients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Surname VARCHAR(255),
       Patronymic VARCHAR(255),
       City VARCHAR(255),
       AdressConnect VARCHAR(255),
       PassportSeries VARCHAR(255),
       PassportNumber VARCHAR(255),
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

    create table Internet.NetworkSwitches (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Mac VARCHAR(255),
       Name VARCHAR(255),
       primary key (Id)
    )

    create table internet.Tariffs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Description VARCHAR(255),
       Price INTEGER,
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

    create table Internet.PartnerAccessSet (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Partner INTEGER UNSIGNED,
       AccessCat INTEGER,
       primary key (Id)
    )

    alter table Internet.RequestsConnection 
        add index (BrigadNumber), 
        add constraint FK6A05CD54B5EBDA3A 
        foreign key (BrigadNumber) 
        references Internet.ConnectBrigads (Id)

    alter table Internet.RequestsConnection 
        add index (ManagerID), 
        add constraint FK6A05CD54EBEC559F 
        foreign key (ManagerID) 
        references internet.Partners (Id)

    alter table Internet.RequestsConnection 
        add index (ClientID), 
        add constraint FK6A05CD54C7F2DBAC 
        foreign key (ClientID) 
        references internet.PhysicalClients (Id)

    alter table internet.PaymentsPhisicalClient 
        add index (ClientId), 
        add constraint FK82F42559C7F2DBAC 
        foreign key (ClientId) 
        references internet.PhysicalClients (Id)

    alter table internet.PaymentsPhisicalClient 
        add index (ManagerID), 
        add constraint FK82F42559EBEC559F 
        foreign key (ManagerID) 
        references internet.Partners (Id)

    alter table Internet.ClientEndpoints 
        add index (Client), 
        add constraint FKBBAEFD1960C6D208 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ClientEndpoints 
        add index (Switch), 
        add constraint FKBBAEFD197F59B68D 
        foreign key (Switch) 
        references Internet.NetworkSwitches (Id)

    alter table internet.PhysicalClients 
        add index (Tariff), 
        add constraint FKDE111BCFE6F8BB1B 
        foreign key (Tariff) 
        references internet.Tariffs (Id)

    alter table internet.PhysicalClients 
        add index (HasRegistered), 
        add constraint FKDE111BCF10E504C1 
        foreign key (HasRegistered) 
        references internet.Partners (Id)

    alter table internet.PhysicalClients 
        add index (HasConnected), 
        add constraint FKDE111BCF1DF06EC1 
        foreign key (HasConnected) 
        references Internet.ConnectBrigads (Id)

    alter table Internet.ConnectBrigads 
        add index (PartnerID), 
        add constraint FK14B0BE5EF29DAF50 
        foreign key (PartnerID) 
        references internet.Partners (Id)

    alter table Internet.PartnerAccessSet 
        add index (Partner), 
        add constraint FKFB2A341A486245DF 
        foreign key (Partner) 
        references internet.Partners (Id)

    alter table Internet.PartnerAccessSet 
        add index (AccessCat), 
        add constraint FKFB2A341A4370E58F 
        foreign key (AccessCat) 
        references internet.AccessCategories (Id)
