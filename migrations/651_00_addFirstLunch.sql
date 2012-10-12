alter table Internet.Clients add column FirstLunch TINYINT(1) default 0 not null;

update internet.clients c
set firstlunch = true
where beginwork is not null and PhysicalClient is not null;
