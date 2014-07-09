alter table internet.Partners add column IsDisabled TINYINT(1) default 0 not null;
alter table Internet.NetworkSwitches add index (Zone), add constraint FK_Internet_NetworkSwitches_Zone foreign key (Zone) references internet.NetworkZones (Id);
alter table internet.ServiceRequest add index (Client), add constraint FK_internet_ServiceRequest_Client foreign key (Client) references Internet.Clients (Id) on delete cascade;
alter table internet.SmsMessages add index (Client), add constraint FK_internet_SmsMessages_Client foreign key (Client) references Internet.Clients (Id) on delete cascade;
