alter table internet.PhysicalClients add column VirtualBalance NUMERIC(19,5);
alter table internet.PhysicalClients add column MoneyBalance NUMERIC(19,5);
alter table internet.WriteOff add column VirtualSum NUMERIC(19,5);
alter table internet.WriteOff add column MoneySum NUMERIC(19,5);
alter table internet.Payments add column Virtual TINYINT(1);

update internet.writeoff set
MoneySum = WriteOffSum;

update internet.physicalclients
set MoneyBalance = Balance;