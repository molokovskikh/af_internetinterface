insert into Internet.TariffChangeRules(FromTariff, ToTariff)
select t.Id, t2.Id
from Internet.Tariffs t, Internet.Tariffs t2
where t.Id <> t2.Id;
