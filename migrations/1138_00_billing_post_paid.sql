alter table Internet.LawyerPerson add column PeriodEnd DATETIME default 0 not null;
alter table Internet.Orders add column IsActivated TINYINT(1) default 0 not null;
alter table Internet.Orders add column IsDeactivated TINYINT(1) default 0 not null;
