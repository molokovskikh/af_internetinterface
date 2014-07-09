update Internet.NetworkZones
set RegionId = (select Id from Internet.Regions where Region = 'Воронеж')
where Name like 'Воронеж';

update Internet.NetworkZones
set RegionId = (select Id from Internet.Regions where Region = 'Борисоглебск')
where Name like 'Борисоглебск';

update Internet.NetworkZones
set RegionId = (select Id from Internet.Regions where Region = 'Борисоглебск')
where Name like 'Борисоглебск-Вокзал24';

update Internet.NetworkZones
set RegionId = (select Id from Internet.Regions where Region = 'Белгород')
where Name like 'Белгород';
