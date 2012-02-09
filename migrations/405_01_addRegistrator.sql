alter table internet.ServiceRequest add column Registrator INTEGER UNSIGNED;
alter table internet.ServiceRequest add column Performer INTEGER UNSIGNED;

insert into internet.usercategories (id,`Comment`, ReductionName) value
(7,"Сервисный инженер", "Service");

insert into internet.categoriesaccessset (categorie, accessCat) values
(7,15);

insert into internet.accesscategories (Name, ReduceName) value
("Работа с сервисными заявками", "SR");

insert into internet.accesscategories (Name, ReduceName) value
("Управление сервисными заявками", "ASR");