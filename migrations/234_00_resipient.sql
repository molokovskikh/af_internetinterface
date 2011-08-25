DROP TABLE IF EXISTS `internet`.`BankPayments`;
CREATE TABLE  `internet`.`BankPayments` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `PayedOn` datetime NOT NULL,
  `Sum` decimal(10,2) NOT NULL DEFAULT '0.00',
  `PayerId` int(10) unsigned DEFAULT NULL,
  `RecipientId` int(10) unsigned DEFAULT NULL,
  `RegistredOn` datetime DEFAULT NULL,
  `Comment` varchar(255) DEFAULT NULL,
  `DocumentNumber` varchar(255) DEFAULT NULL,
  `PayerInn` varchar(255) DEFAULT NULL,
  `PayerName` varchar(255) DEFAULT NULL,
  `PayerAccountCode` varchar(255) DEFAULT NULL,
  `PayerBankDescription` varchar(255) DEFAULT NULL,
  `PayerBankBic` varchar(255) DEFAULT NULL,
  `PayerBankAccountCode` varchar(255) DEFAULT NULL,
  `RecipientInn` varchar(255) DEFAULT NULL,
  `RecipientName` varchar(255) DEFAULT NULL,
  `RecipientAccountCode` varchar(255) DEFAULT NULL,
  `RecipientBankDescription` varchar(255) DEFAULT NULL,
  `RecipientBankBic` varchar(255) DEFAULT NULL,
  `RecipientBankAccountCode` varchar(255) DEFAULT NULL,
  `OperatorComment` varchar(255) DEFAULT NULL,
  `ForAd` tinyint(1) DEFAULT NULL,
  `AdSum` decimal(19,5) DEFAULT NULL,
  `Ad` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=cp1251 ROW_FORMAT=COMPACT;

ALTER TABLE `internet`.`BankPayments` ADD CONSTRAINT `FK_BankPayments_1` FOREIGN KEY `FK_BankPayments_1` (`PayerId`)
    REFERENCES `lawyerperson` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
