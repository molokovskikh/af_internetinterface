alter table Internet.ConnectBrigads add column IsDisabled TINYINT(1) default 0 not null;
alter table Internet.ConnectBrigads add column RegionId INTEGER UNSIGNED;
alter table Internet.ConnectBrigads add index (RegionId), add constraint FK_Internet_ConnectBrigads_RegionId foreign key (RegionId) references internet.Regions (Id);
