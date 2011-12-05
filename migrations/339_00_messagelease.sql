DROP TABLE IF EXISTS `internet`.`sendedleases`;
CREATE TABLE  `internet`.`sendedleases` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `LeaseId` int(10) unsigned NOT NULL,
  `LeasedTo` varchar(255) DEFAULT NULL,
  `LeaseBegin` datetime DEFAULT NULL,
  `LeaseEnd` datetime DEFAULT NULL,
  `Ip` int(10) unsigned DEFAULT NULL,
  `Pool` int(10) unsigned DEFAULT NULL,
  `Module` tinyint(3) unsigned DEFAULT NULL,
  `Port` tinyint(3) unsigned DEFAULT NULL,
  `Switch` int(10) unsigned DEFAULT NULL,
  `SendDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Pool` (`Pool`),
  KEY `Switch` (`Switch`),
  CONSTRAINT `pool` FOREIGN KEY (`Pool`) REFERENCES `ippools` (`Id`),
  CONSTRAINT `switch` FOREIGN KEY (`Switch`) REFERENCES `networkswitches` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251 ROW_FORMAT=COMPACT;