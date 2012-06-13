alter table Internet.AssignedChannels drop foreign key FK_Internet_AssignedChannels_AssignedServiceId;
alter table Internet.AssignedChannels drop foreign key FK_Internet_AssignedChannels_ChannelGroupId;
drop table if exists Internet.IpTvChannelGroups;
drop table if exists Internet.AssignedChannels;
