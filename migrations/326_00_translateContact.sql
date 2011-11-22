insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT c.id, p.PhoneNumber, 0, curdate(), 1 FROM internet.physicalclients p
join internet.Clients c on c.PhysicalCLient = p.id
where PhoneNumber is not null;

insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT c.id, p.HomePhoneNumber, 1, curdate(), 1 FROM internet.physicalclients p
join internet.Clients c on c.PhysicalCLient = p.id
where HomePhoneNumber is not null;

insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT c.id, p.email, 3, curdate(), 1 FROM internet.physicalclients p
join internet.Clients c on c.PhysicalCLient = p.id
where email is not null;

insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT c.id, l.email, 3, curdate(), 1 FROM internet.lawyerperson l
join internet.Clients c on c.lawyerperson = l.id
where email is not null;

insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT c.id, l.Telephone, 0, curdate(), 1 FROM internet.lawyerperson l
join internet.Clients c on c.lawyerperson = l.id
where email is not null;


ALTER TABLE `internet`.`PhysicalClients` DROP COLUMN `PhoneNumber`,
 DROP COLUMN `HomePhoneNumber`,
 DROP COLUMN `Email`;

 ALTER TABLE `internet`.`lawyerperson` DROP COLUMN `Email`,
 DROP COLUMN `Telephone`;

 replace into internet.Contacts
 select id, `client`, REPLACE(Contact, "-", ""), `type`, `Comment`, `Date`, Registrator  from internet.Contacts;
 
replace into internet.Contacts
select id, `client`,
if (ASCII(Contact) = 56, SUBSTRING(Contact, 2), Contact),
`type`, `Comment`, `Date`, Registrator  from internet.Contacts;