update Internet.ClientEndpoints
set IsLoginEnabled = 0;

alter table Internet.ClientEndpoints
modify column IsLoginEnabled tinyint(1) NOT NULL DEFAULT '0';
