
    create table internet.ServiceIterations (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Description VARCHAR(255),
       RegDate DATETIME,
       Performer INTEGER UNSIGNED,
       Request INTEGER UNSIGNED,
       primary key (Id)
    );
alter table internet.ServiceRequest add column ClosedDate DATETIME;
alter table internet.ServiceRequest add column PerformanceDate DATETIME;
