
    
alter table internet.Apartments  drop foreign key FKEE17D25FF58452A5


    
alter table internet.Apartments  drop foreign key FKEE17D25F6E4DAB04


    
alter table Internet.Leases  drop foreign key FK1418AC42B67D86A4


    
alter table Internet.StaticIps  drop foreign key FK997435EBE19718E4


    
alter table Internet.ApartmentHistory  drop foreign key FK2FB6E107458E189E


    
alter table Internet.ApartmentHistory  drop foreign key FK2FB6E107F4415889


    
alter table Internet.LawyerPerson  drop foreign key FK81497F1AAA306B0E


    
alter table Internet.LawyerPerson  drop foreign key FK81497F1A59CF1F44


    
alter table Internet.BankPayments  drop foreign key FKE2D6F6C017F36A57


    
alter table Internet.BankPayments  drop foreign key FKE2D6F6C04E6696F7


    
alter table internet.PaymentsForAgent  drop foreign key FKB487B32FF4415889


    
alter table internet.PaymentsForAgent  drop foreign key FKB487B32F1936F7A4


    
alter table internet.BypassHouses  drop foreign key FKA63BD2D6F58452A5


    
alter table internet.BypassHouses  drop foreign key FKA63BD2D6F4415889


    
alter table internet.StatusCorrelation  drop foreign key FK7317F2D4E118FF4C


    
alter table internet.StatusCorrelation  drop foreign key FK7317F2D443DAC834


    
alter table internet.Partners  drop foreign key FK831741802B993AA9


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07A2F141B3


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07C8B34AD


    
alter table internet.PhysicalClients  drop foreign key FK7ACB2D07D97C7936


    
alter table Internet.CategoriesAccessSet  drop foreign key FKA6B0AC902B993AA9


    
alter table Internet.CategoriesAccessSet  drop foreign key FKA6B0AC90F8536E73


    
alter table internet.Payments  drop foreign key FKA34F0208E19718E4


    
alter table internet.Payments  drop foreign key FKA34F0208ED787E5A


    
alter table Internet.Requests  drop foreign key FKCAAFAEC6A2F141B3


    
alter table Internet.Requests  drop foreign key FKCAAFAEC63F74038D


    
alter table Internet.Requests  drop foreign key FKCAAFAEC6E5098969


    
alter table Internet.Requests  drop foreign key FKCAAFAEC68617A13B


    
alter table internet.Entrances  drop foreign key FK24BE3D07F58452A5


    
alter table internet.Entrances  drop foreign key FK24BE3D07C6A748B7


    
alter table Internet.NetworkSwitches  drop foreign key FKB0EB3E3D9168F203


    
alter table internet.UserWriteOffs  drop foreign key FKC2B6E902E19718E4


    
alter table internet.WriteOff  drop foreign key FK62BB4DADE19718E4


    
alter table Internet.Clients  drop foreign key FKE6F2BBA7F2D481DE


    
alter table Internet.Clients  drop foreign key FKE6F2BBA78D585582


    
alter table Internet.Clients  drop foreign key FKE6F2BBA7E42743DC


    
alter table Internet.Clients  drop foreign key FKE6F2BBA73BC0821B


    
alter table Internet.Clients  drop foreign key FKE6F2BBA7A9D024E1


    
alter table Internet.Clients  drop foreign key FKE6F2BBA73BC08DBC


    
alter table internet.ConnectGraph  drop foreign key FK12DC2F66E19718E4


    
alter table internet.ConnectGraph  drop foreign key FK12DC2F6674157DD6


    
alter table Logs.Internetsessionslogs  drop foreign key FK38EE09DAA4C74BCE


    
alter table internet.Appeals  drop foreign key FK2344849390411CBB


    
alter table internet.Appeals  drop foreign key FK23448493E19718E4


    
alter table Internet.ClientServices  drop foreign key FK85B5D4B9E19718E4


    
alter table Internet.ClientServices  drop foreign key FK85B5D4B97AB3687A


    
alter table Internet.ClientServices  drop foreign key FK85B5D4B9F0EC23CD


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F88E19718E4


    
alter table Internet.ClientEndpoints  drop foreign key FKFCFB5F88C6A748B7


    
alter table internet.Agents  drop foreign key FKA0997E7290411CBB


    drop table if exists internet.Apartments

    drop table if exists Internet.AgentTariffs

    drop table if exists Internet.Leases

    drop table if exists Internet.StaticIps

    drop table if exists internet.PackageSpeed

    drop table if exists Internet.ApartmentHistory

    drop table if exists Internet.LawyerPerson

    drop table if exists Internet.UserCategories

    drop table if exists Internet.BankPayments

    drop table if exists internet.PaymentsForAgent

    drop table if exists internet.BypassHouses

    drop table if exists internet.Status

    drop table if exists internet.StatusCorrelation

    drop table if exists internet.Tariffs

    drop table if exists internet.Partners

    drop table if exists internet.PhysicalClients

    drop table if exists Internet.Services

    drop table if exists Billing.Recipients

    drop table if exists Internet.CategoriesAccessSet

    drop table if exists internet.NetworkZones

    drop table if exists internet.Payments

    drop table if exists internet.AdditionalStatus

    drop table if exists Internet.Requests

    drop table if exists Internet.InternetSettings

    drop table if exists internet.Entrances

    drop table if exists Internet.NetworkSwitches

    drop table if exists Internet.Labels

    drop table if exists Internet.ApartmentStatuses

    drop table if exists internet.AccessCategories

    drop table if exists internet.UserWriteOffs

    drop table if exists internet.Houses

    drop table if exists internet.WriteOff

    drop table if exists Internet.ConnectBrigads

    drop table if exists Internet.Clients

    drop table if exists internet.ConnectGraph

    drop table if exists Logs.Internetsessionslogs

    drop table if exists internet.Appeals

    drop table if exists Internet.ClientServices

    drop table if exists Internet.ClientEndpoints

    drop table if exists internet.Agents

    create table internet.Apartments (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Number INTEGER,
       LastInternet VARCHAR(255),
       LastTV VARCHAR(255),
       PresentInternet VARCHAR(255),
       PresentTV VARCHAR(255),
       Comment VARCHAR(255),
       House INTEGER UNSIGNED,
       Status INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.AgentTariffs (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       ActionName VARCHAR(255),
       Sum NUMERIC(19,5),
       primary key (Id)
    )

    create table Internet.Leases (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       `ip` INTEGER UNSIGNED,
       Endpoint INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.StaticIps (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Ip VARCHAR(255),
       Mask INTEGER,
       Client INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.PackageSpeed (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       PackageId INTEGER,
       Speed INTEGER,
       primary key (Id)
    )

    create table Internet.ApartmentHistory (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       ActionName VARCHAR(255),
       ActionDate DATETIME,
       Apartment INTEGER UNSIGNED,
       Agent INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.LawyerPerson (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       FullName VARCHAR(255),
       ShortName VARCHAR(255),
       LawyerAdress VARCHAR(255),
       ActualAdress VARCHAR(255),
       INN VARCHAR(255),
       Email VARCHAR(255),
       Telephone VARCHAR(255),
       ContactPerson VARCHAR(255),
       Tariff NUMERIC(19,5),
       Balance NUMERIC(19,5),
       MailingAddress VARCHAR(255),
       Speed INTEGER,
       Recipient INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.UserCategories (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Comment VARCHAR(255),
       ReductionName VARCHAR(255),
       primary key (Id)
    )

    create table Internet.BankPayments (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       PayedOn DATETIME,
       Sum NUMERIC(19,5),
       Comment VARCHAR(255),
       DocumentNumber VARCHAR(255),
       RegistredOn DATETIME,
       OperatorComment VARCHAR(255),
       ForAd TINYINT(1),
       AdSum NUMERIC(19,5),
       PayerId INTEGER UNSIGNED,
       RecipientId INTEGER UNSIGNED,
       PayerInn VARCHAR(255),
       PayerName VARCHAR(255),
       PayerAccountCode VARCHAR(255),
       PayerBankDescription VARCHAR(255),
       PayerBankBic VARCHAR(255),
       PayerBankAccountCode VARCHAR(255),
       RecipientInn VARCHAR(255),
       RecipientName VARCHAR(255),
       RecipientAccountCode VARCHAR(255),
       RecipientBankDescription VARCHAR(255),
       RecipientBankBic VARCHAR(255),
       RecipientBankAccountCode VARCHAR(255),
       primary key (Id)
    )

    create table internet.PaymentsForAgent (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Sum NUMERIC(19,5),
       Comment VARCHAR(255),
       RegistrationDate DATETIME,
       Agent INTEGER UNSIGNED,
       Action INTEGER,
       primary key (Id)
    )

    create table internet.BypassHouses (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       BypassDate DATETIME,
       House INTEGER UNSIGNED,
       Agent INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Status (
        Id INTEGER UNSIGNED not null,
       Name VARCHAR(255),
       Blocked TINYINT(1),
       Connected TINYINT(1),
       ManualSet TINYINT(1),
       ShortName VARCHAR(255),
       primary key (Id)
    )

    create table internet.StatusCorrelation (
        StatusId INTEGER UNSIGNED not null,
       AdditionalStatusId INTEGER UNSIGNED not null
    )

    create table internet.Tariffs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Description VARCHAR(255),
       Price INTEGER,
       PackageId INTEGER,
       Hidden TINYINT(1),
       FinalPriceInterval INTEGER,
       FinalPrice NUMERIC(19,5),
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

    create table internet.PhysicalClients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Surname VARCHAR(255),
       Patronymic VARCHAR(255),
       City VARCHAR(255),
       Street VARCHAR(255),
       House INTEGER,
       CaseHouse VARCHAR(255),
       Apartment INTEGER,
       Entrance INTEGER,
       Floor INTEGER,
       PhoneNumber VARCHAR(255),
       HomePhoneNumber VARCHAR(255),
       WhenceAbout VARCHAR(255),
       PassportSeries VARCHAR(255),
       PassportNumber VARCHAR(255),
       PassportDate DATETIME,
       WhoGivePassport VARCHAR(255),
       RegistrationAdress VARCHAR(255),
       Balance NUMERIC(19,5),
       Email VARCHAR(255),
       Password VARCHAR(255),
       ConnectionPaid TINYINT(1),
       DateOfBirth DATETIME,
       ConnectSum NUMERIC(19,5),
       Tariff INTEGER UNSIGNED,
       HouseObj INTEGER UNSIGNED,
       Request INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.Services (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255) not null,
       HumanName VARCHAR(255),
       Price NUMERIC(19,5),
       BlockingAll TINYINT(1),
       primary key (Id)
    )

    create table Billing.Recipients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       FullName VARCHAR(255),
       Address VARCHAR(255),
       INN VARCHAR(255),
       KPP VARCHAR(255),
       BIC VARCHAR(255),
       Bank VARCHAR(255),
       BankLoroAccount VARCHAR(255),
       BankAccountNumber VARCHAR(255),
       Boss VARCHAR(255),
       Accountant VARCHAR(255),
       AccountWarranty VARCHAR(255),
       primary key (Id)
    )

    create table Internet.CategoriesAccessSet (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Categorie INTEGER UNSIGNED,
       AccessCat INTEGER,
       primary key (Id)
    )

    create table internet.NetworkZones (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       primary key (Id)
    )

    create table internet.Payments (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RecievedOn DATETIME,
       PaidOn DATETIME,
       Sum NUMERIC(19,5),
       BillingAccount TINYINT(1),
       Client INTEGER UNSIGNED,
       Agent INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.AdditionalStatus (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ShortName VARCHAR(255),
       primary key (Id)
    )

    create table Internet.Requests (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       ApplicantName VARCHAR(255),
       ApplicantPhoneNumber VARCHAR(255),
       ApplicantEmail VARCHAR(255),
       City VARCHAR(255),
       Street VARCHAR(255),
       House INTEGER,
       CaseHouse VARCHAR(255),
       Apartment INTEGER,
       Entrance INTEGER,
       Floor INTEGER,
       SelfConnect TINYINT(1),
       ActionDate DATETIME,
       RegDate DATETIME,
       VirtualBonus NUMERIC(19,5),
       PaidBonus TINYINT(1),
       Tariff INTEGER UNSIGNED,
       Label INTEGER UNSIGNED,
       Operator INTEGER UNSIGNED,
       Registrator INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.InternetSettings (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       NextBillingDate DATETIME,
       primary key (Id)
    )

    create table internet.Entrances (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Number INTEGER,
       Strut TINYINT(1),
       Cable TINYINT(1),
       House INTEGER UNSIGNED,
       Switch INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.NetworkSwitches (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Mac VARCHAR(255),
       IP VARCHAR(255),
       Name VARCHAR(255),
       PortCount INTEGER,
       Comment VARCHAR(255),
       Zone INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.Labels (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Color VARCHAR(255),
       Deleted TINYINT(1),
       ShortComment VARCHAR(255),
       primary key (Id)
    )

    create table Internet.ApartmentStatuses (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ActivateDate INTEGER,
       ShortName VARCHAR(255),
       primary key (Id)
    )

    create table internet.AccessCategories (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       ReduceName VARCHAR(255),
       primary key (Id)
    )

    create table internet.UserWriteOffs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Sum NUMERIC(19,5),
       Date DATETIME,
       BillingAccount TINYINT(1),
       Comment VARCHAR(255),
       Client INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Houses (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Street VARCHAR(255),
       Number INTEGER,
       CaseHouse VARCHAR(255),
       ApartmentCount INTEGER,
       LastPassDate DATETIME,
       PassCount INTEGER,
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

    create table Internet.Clients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Disabled TINYINT(1),
       Name VARCHAR(255),
       Type INTEGER,
       RatedPeriodDate DATETIME,
       DebtDays INTEGER,
       ShowBalanceWarningPage TINYINT(1),
       BeginWork DATETIME,
       RegDate DATETIME,
       WhoRegisteredName VARCHAR(255),
       WhoConnectedName VARCHAR(255),
       ConnectedDate DATETIME,
       AutoUnblocked TINYINT(1),
       PhysicalClient INTEGER UNSIGNED,
       LawyerPerson INTEGER UNSIGNED,
       WhoRegistered INTEGER UNSIGNED,
       WhoConnected INTEGER UNSIGNED,
       Status INTEGER UNSIGNED,
       AdditionalStatus INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.ConnectGraph (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       IntervalId INTEGER UNSIGNED,
       Day DATETIME,
       Client INTEGER UNSIGNED,
       Brigad INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Logs.Internetsessionslogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       IP VARCHAR(255),
       HwId VARCHAR(255),
       LeaseBegin DATETIME,
       LeaseEnd DATETIME,
       EndpointId INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Appeals (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Appeal VARCHAR(255),
       Date DATETIME,
       AppealType INTEGER,
       Partner INTEGER UNSIGNED,
       Client INTEGER UNSIGNED,
       primary key (Id)
    )

    create table Internet.ClientServices (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       BeginWorkDate DATETIME,
       EndWorkDate DATETIME,
       Activated TINYINT(1),
       Diactivated TINYINT(1),
       Client INTEGER UNSIGNED,
       Service INTEGER UNSIGNED,
       Activator INTEGER UNSIGNED,
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
       MaxLeaseCount INTEGER,
       Pool INTEGER UNSIGNED,
       Client INTEGER UNSIGNED,
       Switch INTEGER UNSIGNED,
       primary key (Id)
    )

    create table internet.Agents (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Partner INTEGER UNSIGNED,
       primary key (Id)
    )

    alter table internet.Apartments 
        add index (House), 
        add constraint FKEE17D25FF58452A5 
        foreign key (House) 
        references internet.Houses (Id)

    alter table internet.Apartments 
        add index (Status), 
        add constraint FKEE17D25F6E4DAB04 
        foreign key (Status) 
        references Internet.ApartmentStatuses (Id)

    alter table Internet.Leases 
        add index (Endpoint), 
        add constraint FK1418AC42B67D86A4 
        foreign key (Endpoint) 
        references Internet.ClientEndpoints (Id)

    alter table Internet.StaticIps 
        add index (Client), 
        add constraint FK997435EBE19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ApartmentHistory 
        add index (Apartment), 
        add constraint FK2FB6E107458E189E 
        foreign key (Apartment) 
        references internet.Apartments (Id)

    alter table Internet.ApartmentHistory 
        add index (Agent), 
        add constraint FK2FB6E107F4415889 
        foreign key (Agent) 
        references internet.Partners (Id)

    alter table Internet.LawyerPerson 
        add index (Speed), 
        add constraint FK81497F1AAA306B0E 
        foreign key (Speed) 
        references internet.PackageSpeed (Id)

    alter table Internet.LawyerPerson 
        add index (Recipient), 
        add constraint FK81497F1A59CF1F44 
        foreign key (Recipient) 
        references Billing.Recipients (Id)

    alter table Internet.BankPayments 
        add index (PayerId), 
        add constraint FKE2D6F6C017F36A57 
        foreign key (PayerId) 
        references Internet.LawyerPerson (Id)

    alter table Internet.BankPayments 
        add index (RecipientId), 
        add constraint FKE2D6F6C04E6696F7 
        foreign key (RecipientId) 
        references Billing.Recipients (Id)

    alter table internet.PaymentsForAgent 
        add index (Agent), 
        add constraint FKB487B32FF4415889 
        foreign key (Agent) 
        references internet.Partners (Id)

    alter table internet.PaymentsForAgent 
        add index (Action), 
        add constraint FKB487B32F1936F7A4 
        foreign key (Action) 
        references Internet.AgentTariffs (Id)

    alter table internet.BypassHouses 
        add index (House), 
        add constraint FKA63BD2D6F58452A5 
        foreign key (House) 
        references internet.Houses (Id)

    alter table internet.BypassHouses 
        add index (Agent), 
        add constraint FKA63BD2D6F4415889 
        foreign key (Agent) 
        references internet.Partners (Id)

    alter table internet.StatusCorrelation 
        add index (AdditionalStatusId), 
        add constraint FK7317F2D4E118FF4C 
        foreign key (AdditionalStatusId) 
        references internet.AdditionalStatus (Id)

    alter table internet.StatusCorrelation 
        add index (StatusId), 
        add constraint FK7317F2D443DAC834 
        foreign key (StatusId) 
        references internet.Status (Id)

    alter table internet.Partners 
        add index (Categorie), 
        add constraint FK831741802B993AA9 
        foreign key (Categorie) 
        references Internet.UserCategories (Id)

    alter table internet.PhysicalClients 
        add index (Tariff), 
        add constraint FK7ACB2D07A2F141B3 
        foreign key (Tariff) 
        references internet.Tariffs (Id)

    alter table internet.PhysicalClients 
        add index (HouseObj), 
        add constraint FK7ACB2D07C8B34AD 
        foreign key (HouseObj) 
        references internet.Houses (Id)

    alter table internet.PhysicalClients 
        add index (Request), 
        add constraint FK7ACB2D07D97C7936 
        foreign key (Request) 
        references Internet.Requests (Id)

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

    alter table internet.Payments 
        add index (Client), 
        add constraint FKA34F0208E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table internet.Payments 
        add index (Agent), 
        add constraint FKA34F0208ED787E5A 
        foreign key (Agent) 
        references internet.Agents (Id)

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

    alter table Internet.Requests 
        add index (Registrator), 
        add constraint FKCAAFAEC68617A13B 
        foreign key (Registrator) 
        references internet.Partners (Id)

    alter table internet.Entrances 
        add index (House), 
        add constraint FK24BE3D07F58452A5 
        foreign key (House) 
        references internet.Houses (Id)

    alter table internet.Entrances 
        add index (Switch), 
        add constraint FK24BE3D07C6A748B7 
        foreign key (Switch) 
        references Internet.NetworkSwitches (Id)

    alter table Internet.NetworkSwitches 
        add index (Zone), 
        add constraint FKB0EB3E3D9168F203 
        foreign key (Zone) 
        references internet.NetworkZones (Id)

    alter table internet.UserWriteOffs 
        add index (Client), 
        add constraint FKC2B6E902E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table internet.WriteOff 
        add index (Client), 
        add constraint FK62BB4DADE19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.Clients 
        add index (PhysicalClient), 
        add constraint FKE6F2BBA7F2D481DE 
        foreign key (PhysicalClient) 
        references internet.PhysicalClients (Id)

    alter table Internet.Clients 
        add index (LawyerPerson), 
        add constraint FKE6F2BBA78D585582 
        foreign key (LawyerPerson) 
        references Internet.LawyerPerson (Id)

    alter table Internet.Clients 
        add index (WhoRegistered), 
        add constraint FKE6F2BBA7E42743DC 
        foreign key (WhoRegistered) 
        references internet.Partners (Id)

    alter table Internet.Clients 
        add index (WhoConnected), 
        add constraint FKE6F2BBA73BC0821B 
        foreign key (WhoConnected) 
        references Internet.ConnectBrigads (Id)

    alter table Internet.Clients 
        add index (Status), 
        add constraint FKE6F2BBA7A9D024E1 
        foreign key (Status) 
        references internet.Status (Id)

    alter table Internet.Clients 
        add index (AdditionalStatus), 
        add constraint FKE6F2BBA73BC08DBC 
        foreign key (AdditionalStatus) 
        references internet.AdditionalStatus (Id)

    alter table internet.ConnectGraph 
        add index (Client), 
        add constraint FK12DC2F66E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table internet.ConnectGraph 
        add index (Brigad), 
        add constraint FK12DC2F6674157DD6 
        foreign key (Brigad) 
        references Internet.ConnectBrigads (Id)

    alter table Logs.Internetsessionslogs 
        add index (EndpointId), 
        add constraint FK38EE09DAA4C74BCE 
        foreign key (EndpointId) 
        references Internet.ClientEndpoints (Id)

    alter table internet.Appeals 
        add index (Partner), 
        add constraint FK2344849390411CBB 
        foreign key (Partner) 
        references internet.Partners (Id)

    alter table internet.Appeals 
        add index (Client), 
        add constraint FK23448493E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ClientServices 
        add index (Client), 
        add constraint FK85B5D4B9E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ClientServices 
        add index (Service), 
        add constraint FK85B5D4B97AB3687A 
        foreign key (Service) 
        references Internet.Services (Id)

    alter table Internet.ClientServices 
        add index (Activator), 
        add constraint FK85B5D4B9F0EC23CD 
        foreign key (Activator) 
        references internet.Partners (Id)

    alter table Internet.ClientEndpoints 
        add index (Client), 
        add constraint FKFCFB5F88E19718E4 
        foreign key (Client) 
        references Internet.Clients (Id)

    alter table Internet.ClientEndpoints 
        add index (Switch), 
        add constraint FKFCFB5F88C6A748B7 
        foreign key (Switch) 
        references Internet.NetworkSwitches (Id)

    alter table internet.Agents 
        add index (Partner), 
        add constraint FKA0997E7290411CBB 
        foreign key (Partner) 
        references internet.Partners (Id)
