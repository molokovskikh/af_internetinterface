update internet.clients c
join internet.LawyerPerson l on l.id = c.LawyerPerson
set c.name = l.ShortName;
