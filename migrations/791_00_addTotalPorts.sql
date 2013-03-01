alter table Internet.NetworkSwitches add column TotalPorts INTEGER default 0 not null;

update Internet.NetworkSwitches set TotalPorts = 24;