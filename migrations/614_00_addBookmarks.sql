
    create table internet.Bookmarks (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Text VARCHAR(255),
       Date DATETIME default 0  not null,
       primary key (Id)
    );
