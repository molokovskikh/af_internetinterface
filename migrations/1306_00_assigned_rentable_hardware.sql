alter table Internet.ClientServices add column RentableHardware INTEGER UNSIGNED;
alter table Internet.ClientServices add index (RentableHardware), add constraint FK_Internet_ClientServices_RentableHardware foreign key (RentableHardware) references Internet.RentableHardwares (Id);
