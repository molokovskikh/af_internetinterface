use Internet;
insert into inforoom2_dealers (Employee)
  select P.Id
  from partners P
  where P.Categorie = 1;