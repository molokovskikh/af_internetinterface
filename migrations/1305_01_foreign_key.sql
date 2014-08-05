alter table Internet.Invoices add index (Client), add constraint FK_Internet_Invoices_Client foreign key (Client) references Internet.Clients (Id) on delete set null;
alter table internet.ServiceIterations add index (Request), add constraint FK_internet_ServiceIterations_Request foreign key (Request) references internet.ServiceRequest (Id) on delete cascade;
alter table internet.MessagesForClients add index (Client), add constraint FK_internet_MessagesForClients_Client foreign key (Client) references Internet.Clients (Id) on delete cascade;
