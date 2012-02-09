    create table internet.ServiceRequest (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Description VARCHAR(255),
       Contact VARCHAR(255),
       RegDate DATETIME,
       Status INTEGER UNSIGNED,
       Client INTEGER UNSIGNED,
       primary key (Id)
    );

	ALTER TABLE `internet`.`servicerequeststatuses` MODIFY COLUMN `Active` TINYINT(1) NOT NULL;