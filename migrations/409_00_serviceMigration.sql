
    create table internet.ServiceRequest (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Description VARCHAR(255),
       Contact VARCHAR(255),
       Status INTEGER,
       RegDate DATETIME,
       ClosedDate DATETIME,
       PerformanceDate DATETIME,
       Sum NUMERIC(19,5),
       Free TINYINT(1),
       Client INTEGER UNSIGNED,
       Registrator INTEGER UNSIGNED,
       Performer INTEGER UNSIGNED,
       primary key (Id)
    );

    create table internet.ServiceIterations (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Description VARCHAR(255),
       RegDate DATETIME,
       Performer INTEGER UNSIGNED,
       Request INTEGER UNSIGNED,
       primary key (Id)
    );
