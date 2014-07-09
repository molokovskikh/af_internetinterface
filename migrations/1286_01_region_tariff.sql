alter table internet.Tariffs add column RegionId INTEGER UNSIGNED;
alter table internet.Tariffs add index (RegionId), add constraint FK_internet_Tariffs_RegionId foreign key (RegionId) references internet.Regions (Id) on delete set null;
