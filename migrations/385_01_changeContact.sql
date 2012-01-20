update internet.Clients c
set c.SendSmsNotifocation = true;

insert into internet.Contacts (`client`, Contact, `Type`, `Date`, Registrator)
SELECT `client`, Contact, 6, Curdate(), 1 FROM internet.contacts c
where `Type` = 0;