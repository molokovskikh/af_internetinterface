alter table Internet.InvoiceParts add index (Invoice), add constraint FK_Internet_InvoiceParts_Invoice foreign key (Invoice) references Internet.Invoices (Id) on delete cascade;
alter table Internet.ClientEndpoints add index (WhoConnected), add constraint FK_Internet_ClientEndpoints_WhoConnected foreign key (WhoConnected) references Internet.ConnectBrigads (Id) on delete set null;
alter table internet.WriteOff add index (Service), add constraint FK_internet_WriteOff_Service foreign key (Service) references Internet.OrderServices (Id) on delete set null;
alter table Internet.Invoices add index (Client), add constraint FK_Internet_Invoices_Client foreign key (Client) references Internet.Clients (Id) on delete set null;
alter table internet.ServiceIterations add index (Request), add constraint FK_internet_ServiceIterations_Request foreign key (Request) references internet.ServiceRequest (Id) on delete cascade;
alter table internet.MessagesForClients add index (Client), add constraint FK_internet_MessagesForClients_Client foreign key (Client) references Internet.Clients (Id) on delete cascade;
