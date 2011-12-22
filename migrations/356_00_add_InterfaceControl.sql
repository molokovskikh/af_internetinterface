alter table Internet.Services add column InterfaceControl TINYINT(1);

update internet.Services s
set s.InterfaceControl = 1
where s.id = 1;

update internet.Services s
set s.InterfaceControl = 0
where s.id = 3;
