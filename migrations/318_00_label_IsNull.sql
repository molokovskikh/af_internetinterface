update internet.requests r set
r.`label` = null
where r.`label` = 0;

update internet.requests r
set r.`label` = 47
where r.actiondate >= '2011-10-17' and r.`label` is null;