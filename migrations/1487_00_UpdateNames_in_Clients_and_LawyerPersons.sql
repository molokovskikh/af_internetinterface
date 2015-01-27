update internet.Clients as Cl inner join internet.LawyerPerson as LP on Cl.LawyerPerson = LP.Id
set Cl.Name = REPLACE(Cl.Name,'Расторгнут','РАСТОРГНУТ'), LP.ShortName = REPLACE(LP.ShortName,'Расторгнут','РАСТОРГНУТ')
where LP.ShortName like 'Расторгнут%'