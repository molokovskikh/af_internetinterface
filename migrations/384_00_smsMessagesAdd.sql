
    create table internet.SmsMessages (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       CreateDate DATETIME,
       SendToOperatorDate DATETIME,
       ShouldBeSend DATETIME,
       Text VARCHAR(255),
       PhoneNumber VARCHAR(255),
       IsSended TINYINT(1),
       SMSID VARCHAR(255),
       Client INTEGER UNSIGNED,
       primary key (Id)
    );
