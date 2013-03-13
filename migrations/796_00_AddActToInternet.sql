CREATE TABLE  `internet`.`acts` (

`Id` int(10) unsigned NOT NULL AUTO_INCREMENT,

`Period` char(8) DEFAULT NULL,

`ActDate` datetime DEFAULT NULL,

`Recipient` int(10) unsigned DEFAULT NULL,

`Sum` decimal(19,5) DEFAULT NULL,

`PayerName` varchar(255) DEFAULT NULL,

`Customer` varchar(255) DEFAULT NULL,
`Client` INTEGER UNSIGNED,
 `OrderId` INTEGER UNSIGNED,

`CreatedOn` datetime DEFAULT '0001-01-01 00:00:00',

PRIMARY KEY (`Id`),
CONSTRAINT `FK_acts_client` FOREIGN KEY `FK_acts_client` (`Client`)
    REFERENCES `clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT,
 CONSTRAINT `FK_acts_orders` FOREIGN KEY `FK_acts_orders` (`OrderId`)
    REFERENCES `orders` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT)
ENGINE=InnoDB;
