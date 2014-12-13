update internet.tariffs set _IsArchived = 1;
update internet.tariffs set _IsArchived = 0, _IsServicePlan = 0
where id =45 or id = 49 or id = 81 or id =79;