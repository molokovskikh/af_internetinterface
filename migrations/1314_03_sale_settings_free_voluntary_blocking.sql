alter table internet.SaleSettings add column FreeDaysVoluntaryBlocking INTEGER default 0 not null;
update internet.SaleSettings set FreeDaysVoluntaryBlocking = 28;
