    create table Internet.PaymentCards (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Serial VARCHAR(255),
       Pin VARCHAR(255),
       Sum NUMERIC(19,5),
       GenerateAt DATETIME,
       primary key (Id)
    );
