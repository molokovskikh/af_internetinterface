create table Internet.UploadDocs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Filename VARCHAR(255),
       AssignedService INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Internet.UploadDocs add index (AssignedService), add constraint FK_Internet_UploadDocs_AssignedService foreign key (AssignedService) references Internet.ClientServices (Id) on delete cascade;
