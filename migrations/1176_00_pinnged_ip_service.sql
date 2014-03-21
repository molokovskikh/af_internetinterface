alter table Internet.CategoriesAccessSet add index (Categorie), add constraint FK_Internet_CategoriesAccessSet_Categorie foreign key (Categorie) references Internet.UserCategories (Id) on delete cascade;
alter table Internet.CategoriesAccessSet add index (AccessCat), add constraint FK_Internet_CategoriesAccessSet_AccessCat foreign key (AccessCat) references internet.AccessCategories (Id) on delete cascade;
alter table Internet.ClientServices add column IsFree TINYINT(1) default 0 not null;
alter table Internet.ClientServices add column Endpoint INTEGER UNSIGNED;
alter table Internet.ClientServices add index (Endpoint), add constraint FK_Internet_ClientServices_Endpoint foreign key (Endpoint) references Internet.ClientEndpoints (Id) on delete cascade;
