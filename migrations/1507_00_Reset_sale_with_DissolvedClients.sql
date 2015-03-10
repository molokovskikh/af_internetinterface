update internet.clients Cl
set Cl.Sale = 0
where Cl.`Status` = 10 and Cl.Sale > 0;
