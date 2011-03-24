
    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07A2F141B3


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D0782A29C0E


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07C1F8E24E


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07A9D024E1


    
alter table Internet.Requests  drop foreign key FKCAAFAEC6A2F141B3


    
alter table Internet.Requests  drop foreign key FKCAAFAEC63F74038D


    
alter table Internet.Requests  drop foreign key FKCAAFAEC6E5098969


    
alter table internet.Partners  drop foreign key FK831741802B993AA9


    
alter table Internet.NetworkSwitches  drop foreign key FKB0EB3E3D9168F203


    
alter table internet.WriteOff  drop foreign key FK62BB4DAD3F7618E4


    
alter table internet.Payments  drop foreign key FKA34F020838F0897E


    
alter table internet.Payments  drop foreign key FKA34F0208ED787E5A


    
alter table Internet.Clients  drop foreign key FKE6F2BBA7224E056


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F883F7618E4


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F88C6A748B7


    
alter table Internet.CategoriesAccessSet  drop foreign key FKA6B0AC902B993AA9


    
alter table Internet.CategoriesAccessSet  drop foreign key FKA6B0AC90F8536E73


    
alter table internet.Appeals  drop foreign key FK2344849390411CBB


    
alter table internet.Appeals  drop foreign key FK234484936B79726


    
alter table Internet.Lease  drop foreign key FK13C7AC42B67D86A4


    
alter table internet.PaymentForConnect  drop foreign key FK32DE3CAABE8ECE31


    
alter table internet.PaymentForConnect  drop foreign key FK32DE3CAAE588D46A


    
alter table internet.Agents  drop foreign key FKA0997E7290411CBB


    drop table if exists internet.PhysicalClients

    drop table if exists internet.NetworkZones

    drop table if exists Internet.Requests

    drop table if exists internet.Status

    drop table if exists internet.Partners

    drop table if exists Internet.NetworkSwitches

    drop table if exists internet.WriteOff

    drop table if exists Internet.ConnectBrigads

    drop table if exists internet.Payments

    drop table if exists Internet.Clients

    drop table if exists Internet.ClientEndpoints

    drop table if exists Internet.UserCategories

    drop table if exists Internet.InternetSettings

    drop table if exists internet.Tariffs

    drop table if exists Internet.CategoriesAccessSet

    drop table if exists internet.Appeals

    drop table if exists Internet.Lease

    drop table if exists internet.PaymentForConnect

    drop table if exists internet.AccessCategories

    drop table if exists Internet.Labels

    drop table if exists internet.Agents

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
       Balance NUMERIC(19,5),
       Email VARCHAR(255),
       Password VARCHAR(255),
       Connected TINYINT(1),
       ConnectedDate DATETIME,
       AutoUnblocked TINYINT(1),
       Tariff INTEGER UNSIGNED,
       HasRegistered INTEGER UNSIGNED,
       HasConnected INTEGER UNSIGNED,
       Status INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.NetworkZones (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       primary key (Id)
    )

    create table Internet.Requests (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       ApplicantName VARCHAR(255),
       ApplicantPhoneNumber VARCHAR(255),
       ApplicantEmail VARCHAR(255),
       City VARCHAR(255),
       Street VARCHAR(255),
       House VARCHAR(255),
       CaseHouse VARCHAR(255),
       Apartment VARCHAR(255),
       Entrance VARCHAR(255),
       Floor VARCHAR(255),
       SelfConnect TINYINT(1),
       ActionDate DATETIME,
       Tariff INTEGER UNSIGNED,
       Label INTEGER UNSIGNED,
       Operator INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Status (
        Id INTEGER UNSIGNED not null,
       Name VARCHAR(255),
       Blocked TINYINT(1),
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
       Categorie INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.NetworkSwitches (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Mac VARCHAR(255),
       IP VARCHAR(255),
       Name VARCHAR(255),
       Zone INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.WriteOff (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       WriteOffSum NUMERIC(19,5),
       WriteOffDate DATETIME,
       Client INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.ConnectBrigads (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       primary key (Id)
    )

    create table internet.Payments (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RecievedOn DATETIME,
       PaidOn DATETIME,
       Sum VARCHAR(255),
       BillingAccount TINYINT(1),
       Client INTEGER UNSIGNED,
       Agent INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.Clients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Disabled TINYINT(1),
       Name VARCHAR(255),
       Type INTEGER,
       RatedPeriodDate DATETIME,
       DebtDays INTEGER,
       FirstLease TINYINT(1),
       ShowBalanceWarningPage TINYINT(1),
       SayDhcpIsNewClient TINYINT(1),
       SayBillingIsNewClient TINYINT(1),
       PhisicalClient INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.ClientEndpoints (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Disabled TINYINT(1),
       Ip VARCHAR(255),
       Module INTEGER,
       Port INTEGER,
       Monitoring TINYINT(1),
       PackageId INTEGER,
       Client INTEGER UNSIGNED,
       Switch INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.UserCategories (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Comment VARCHAR(255),
       ReductionName VARCHAR(255),
       primary key (Id)
    )

    create table Internet.InternetSettings (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       NextBillingDate DATETIME,
       primary key (Id)
    )

    create table internet.Tariffs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Description VARCHAR(255),
       Price INTEGER,
       PackageId INTEGER,
       primary key (Id)
    )

    create table Internet.CategoriesAccessSet (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Categorie INTEGER UNSIGNED,
       AccessCat INTEGER,
       primary key (Id)
    )

    create table internet.Appeals (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Appeal VARCHAR(255),
       Date DATETIME,
       Partner INTEGER UNSIGNED,
       PhysicalClient INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.Lease (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       `ip` INTEGER UNSIGNED,
       Endpoint INTEGER UNSIGNED,
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

    create table Internet.Labels (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Color VARCHAR(255),
       primary key (Id)
    )

    create table internet.Agents (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Partner INTEGER UNSIGNED,
       primary key (Id)
    )

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

    alter table internet.PhysicalClients 
        add index (Status), 
        add constraint FK7ACB2D07A9D024E1 
        foreign key (Status) 
        references internet.Status (Id)

    alter table Internet.Requests 
        add index (Tariff), 
        add constraint FKCAAFAEC6A2F141B3 
        foreign key (Tariff) 
        references internet.Tariffs (Id)

    alter table Internet.Requests 
        add index (Label), 
        add constraint FKCAAFAEC63F74038D 
        foreign key (Label) 
        references Internet.Labels (Id)

    alter table Internet.Requests 
        add index (Operator), 
        add constraint FKCAAFAEC6E5098969 
        foreign key (Operator) 
        references internet.Partners (Id)

    alter table internet.Partners 
        add index (Categorie), 
        add constraint FK831741802B993AA9 
        foreign key (Categorie) 
        references Internet.UserCategories (Id)

    alter table Internet.NetworkSwitches 
        add index (Zone), 
        add constraint FKB0EB3E3D9168F203 
        foreign key (Zone) 
        references internet.NetworkZones (Id)

    alter table internet.WriteOff 
        add index (Client), 
        add constraint FK62BB4DAD3F7618E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table internet.Payments 
        add index (Client), 
        add constraint FKA34F020838F0897E 
        foreign key (Client) 
        references internet.PhysicalClients (Id)

    alter table internet.Payments 
        add index (Agent), 
        add constraint FKA34F0208ED787E5A 
        foreign key (Agent) 
        references internet.Agents (Id)

    alter table Internet.Clients 
        add index (PhisicalClient), 
        add constraint FKE6F2BBA7224E056 
        foreign key (PhisicalClient) 
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

    alter table Internet.CategoriesAccessSet 
        add index (Categorie), 
        add constraint FKA6B0AC902B993AA9 
        foreign key (Categorie) 
        references Internet.UserCategories (Id)

    alter table Internet.CategoriesAccessSet 
        add index (AccessCat), 
        add constraint FKA6B0AC90F8536E73 
        foreign key (AccessCat) 
        references internet.AccessCategories (Id)

    alter table internet.Appeals 
        add index (Partner), 
        add constraint FK2344849390411CBB 
        foreign key (Partner) 
        references internet.Partners (Id)

    alter table internet.Appeals 
        add index (PhysicalClient), 
        add constraint FK234484936B79726 
        foreign key (PhysicalClient) 
        references internet.PhysicalClients (Id)

    alter table Internet.Lease 
        add index (Endpoint), 
        add constraint FK13C7AC42B67D86A4 
        foreign key (Endpoint) 
        references Internet.ClientEndpoints (Id)

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

    alter table internet.Agents 
        add index (Partner), 
        add constraint FKA0997E7290411CBB 
        foreign key (Partner) 
        references internet.Partners (Id)
