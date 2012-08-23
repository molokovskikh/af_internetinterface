DROP TABLE IF EXISTS `analit`.`Logs`;
CREATE TABLE  `analit`.`Logs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `Date` datetime NOT NULL,
  `Level` varchar(50) NOT NULL,
  `Logger` varchar(255) NOT NULL,
  `Host` varchar(255) DEFAULT NULL,
  `User` varchar(255) DEFAULT NULL,
  `Message` text,
  `Exception` text,
  `App` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Date` (`Date`)
) ENGINE=MyISAM AUTO_INCREMENT=2463 DEFAULT CHARSET=cp1251;