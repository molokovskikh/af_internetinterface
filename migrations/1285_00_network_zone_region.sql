alter table internet.NetworkZones add column RegionId INTEGER UNSIGNED;
alter table internet.NetworkZones add index (RegionId), add constraint FK_internet_NetworkZones_RegionId foreign key (RegionId) references internet.Regions (Id) on delete set null;
