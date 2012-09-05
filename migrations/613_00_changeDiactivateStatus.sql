update internet.ClientServices set
diactivated = 1
where service = 1
and activated = false
and diactivated = false;