
    create table Internet.IpTvChannelGroups (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       CostPerMonth NUMERIC(19,5) default 0  not null,
       CostPerMonthWithInternet NUMERIC(19,5) default 0  not null,
       primary key (Id)
    );

    create table Internet.AssignedChannels (
        AssignedServiceId INTEGER UNSIGNED not null,
       ChannelGroupId INTEGER UNSIGNED not null
    );
alter table Internet.ClientServices add column ActivatedByUser TINYINT(1) default 0 not null;
alter table Internet.AssignedChannels add index (ChannelGroupId), add constraint FK_Internet_AssignedChannels_ChannelGroupId foreign key (ChannelGroupId) references Internet.IpTvChannelGroups (Id) on delete cascade;
alter table Internet.AssignedChannels add index (AssignedServiceId), add constraint FK_Internet_AssignedChannels_AssignedServiceId foreign key (AssignedServiceId) references Internet.Services (Id) on delete cascade;
