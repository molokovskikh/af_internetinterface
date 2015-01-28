update internet.Clients as Cl inner join internet.LawyerPerson as LP on Cl.LawyerPerson = LP.Id
set Cl.Name = TRIM(SUBSTRING_INDEX(Cl.Name, 'РАСТОРГНУТ', -1)), LP.ShortName = TRIM(SUBSTRING_INDEX(LP.ShortName, 'РАСТОРГНУТ', -1))
where LP.ShortName like 'РАСТОРГНУТ%'