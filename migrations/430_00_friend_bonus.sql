alter table Internet.Requests add column PaidFriendBonus TINYINT(1) not null;
alter table Internet.Requests add column FriendThisClient INTEGER UNSIGNED;

update internet.menufield set
`name` = "Оферта"
where `name` = "Договор оферта";