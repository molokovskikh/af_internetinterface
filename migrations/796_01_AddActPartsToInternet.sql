CREATE TABLE  `internet`.`actparts` (

`Id` int(10) unsigned NOT NULL AUTO_INCREMENT,

`Name` varchar(255) DEFAULT NULL,

`Cost` decimal(19,5) DEFAULT NULL,

`Count` int(11) DEFAULT NULL,

`Act` int(10) unsigned DEFAULT NULL,

PRIMARY KEY (`Id`),

KEY `FK_ActsParts_Act` (`Act`),

CONSTRAINT `FK_ActsParts_Act` FOREIGN KEY (`Act`) REFERENCES `acts` (`Id`) ON DELETE CASCADE
)
ENGINE=InnoDB;