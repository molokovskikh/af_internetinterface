DROP TEMPORARY TABLE IF EXISTS internet.SaleUpdate;

CREATE TEMPORARY TABLE internet.SaleUpdate (
clientid INT unsigned,
newSale INT unsigned) engine=MEMORY ;

insert into internet.SaleUpdate (clientid, newSale)
select c.id, period_diff(date_format(now(), '%Y%m'), date_format(c.StartNoBlock, '%Y%m')) as newSale
from internet.clients c
where
period_diff(date_format(now(), '%Y%m'), date_format(StartNoBlock, '%Y%m')) < sale and period_diff(date_format(now(), '%Y%m'), date_format(StartNoBlock, '%Y%m')) < 3
;

update internet.Clients c
join internet.SaleUpdate sn on sn.Clientid = c.id
set c.Sale = sn.newSale;