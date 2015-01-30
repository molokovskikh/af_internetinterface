update internet.Clients as Cl inner join internet.LawyerPerson as LP on Cl.LawyerPerson = LP.Id
set Cl.Name = TRIM(SUBSTRING_INDEX(Cl.Name, 'РАСТОРНУТ', -1)), LP.ShortName = TRIM(SUBSTRING_INDEX(LP.ShortName, 'РАСТОРНУТ', -1))
where Cl.Id = 6939