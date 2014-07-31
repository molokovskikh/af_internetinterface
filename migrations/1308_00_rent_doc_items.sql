create table Internet.RentDocItems (
        Id INTEGER NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Count INTEGER default 0  not null,
       RentableHardware INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Internet.RentDocItems add index (RentableHardware), add constraint FK_Internet_RentDocItems_RentableHardware foreign key (RentableHardware) references Internet.RentableHardwares (Id);
