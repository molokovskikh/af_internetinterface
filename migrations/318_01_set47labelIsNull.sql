update internet.requests r
set r.`label` = null
where r.actiondate >= '2011-10-18' and r.`label` = 47;