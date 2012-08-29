
    create table internet.MessagesForClients (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Text VARCHAR(255),
       Enabled TINYINT(1),
       EndDate DATETIME,
       RegDate DATETIME,
       Client INTEGER UNSIGNED,
       Registrator INTEGER UNSIGNED,
       primary key (Id)
    );
