alter table internet.SmsMessages add index (Registrator), add constraint FK_internet_SmsMessages_Registrator foreign key (Registrator) references internet.Partners (Id) on delete set null;

alter table internet.Partners add column ShowContractOfAgency TINYINT(1) default 0 not null;
