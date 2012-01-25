delete FROM c
using internet.contacts c
join internet.CLients cl on cl.id = c.Client and cl.LawyerPerson is not null
where c.type = 6;