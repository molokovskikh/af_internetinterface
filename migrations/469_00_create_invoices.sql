
    create table Internet.InvoiceParts (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Cost NUMERIC(19,5) default 0  not null,
       Count INTEGER UNSIGNED default 0  not null,
       Invoice INTEGER UNSIGNED,
       primary key (Id)
    );

    create table Internet.Invoices (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Sum NUMERIC(19,5) default 0  not null,
       PayerName VARCHAR(255),
       Customer VARCHAR(255),
       Date DATETIME default 0  not null,
       Client INTEGER UNSIGNED,
       Recipient INTEGER UNSIGNED,
       primary key (Id)
    );
