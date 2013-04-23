alter table internet.PhysicalClients add column ExternalClientId INTEGER;
alter table internet.NetworkZones add column IsSelfRegistrationEnabled TINYINT(1) default 0 not null;
