alter table internet.SaleSettings add column DaysForRepair INTEGER default 0 not null;
update internet.SaleSettings set DaysForRepair = 3;
