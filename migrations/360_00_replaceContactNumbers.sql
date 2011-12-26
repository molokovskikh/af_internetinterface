replace into internet.Contacts
select Id, `client`,
if (length(contact) = 11,
REPLACE(contact, "-", ""),
contact),
`type`, `comment`, `date`, Registrator from internet.Contacts;
