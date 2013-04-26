update internet.ClientEndPoints ce
join internet.clients c on ce.Client = c.id
set
ce.WhoConnected = c.WhoConnected;