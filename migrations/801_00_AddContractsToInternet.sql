CREATE TABLE  `internet`.`contracts` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Date` datetime DEFAULT NULL,
  `Customer` varchar(255) DEFAULT NULL,
  `OrderId` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_contracts_orders` (`OrderId`),
  CONSTRAINT `FK_contracts_orders` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB;