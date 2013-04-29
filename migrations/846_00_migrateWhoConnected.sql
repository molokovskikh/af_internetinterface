alter table Internet.ClientEndpoints add column WhoConnected INTEGER UNSIGNED;

update internet.ClientEndPoints ce
join internet.clients c on ce.Client = c.id
set
ce.WhoConnected = c.WhoConnected;