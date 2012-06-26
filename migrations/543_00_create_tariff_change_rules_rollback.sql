alter table Internet.TariffChangeRules drop foreign key FK_Internet_TariffChangeRules_ToTariff;
alter table Internet.TariffChangeRules drop foreign key FK_Internet_TariffChangeRules_FromTariff;
drop table if exists Internet.TariffChangeRules;
