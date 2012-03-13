
CREATE TABLE  `logs`.`BankPaymentInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `PaymentId` int(10) unsigned not null,
  `PayedOn` datetime,
  `Sum` decimal(10,2),
  `PayerId` int(10) unsigned,
  `RecipientId` int(10) unsigned,
  `RegistredOn` datetime,
  `Comment` varchar(255),
  `DocumentNumber` varchar(255),
  `PayerInn` varchar(255),
  `PayerName` varchar(255),
  `PayerAccountCode` varchar(255),
  `PayerBankDescription` varchar(255),
  `PayerBankBic` varchar(255),
  `PayerBankAccountCode` varchar(255),
  `RecipientInn` varchar(255),
  `RecipientName` varchar(255),
  `RecipientAccountCode` varchar(255),
  `RecipientBankDescription` varchar(255),
  `RecipientBankBic` varchar(255),
  `RecipientBankAccountCode` varchar(255),
  `OperatorComment` varchar(255),
  `ForAd` tinyint(1),
  `AdSum` decimal(19,5),
  `Ad` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.BankPaymentLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BankPaymentLogDelete AFTER DELETE ON internet.BankPayments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.BankPaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		PaymentId = OLD.Id,
		PayedOn = OLD.PayedOn,
		Sum = OLD.Sum,
		PayerId = OLD.PayerId,
		RecipientId = OLD.RecipientId,
		RegistredOn = OLD.RegistredOn,
		Comment = OLD.Comment,
		DocumentNumber = OLD.DocumentNumber,
		PayerInn = OLD.PayerInn,
		PayerName = OLD.PayerName,
		PayerAccountCode = OLD.PayerAccountCode,
		PayerBankDescription = OLD.PayerBankDescription,
		PayerBankBic = OLD.PayerBankBic,
		PayerBankAccountCode = OLD.PayerBankAccountCode,
		RecipientInn = OLD.RecipientInn,
		RecipientName = OLD.RecipientName,
		RecipientAccountCode = OLD.RecipientAccountCode,
		RecipientBankDescription = OLD.RecipientBankDescription,
		RecipientBankBic = OLD.RecipientBankBic,
		RecipientBankAccountCode = OLD.RecipientBankAccountCode,
		OperatorComment = OLD.OperatorComment,
		ForAd = OLD.ForAd,
		AdSum = OLD.AdSum,
		Ad = OLD.Ad;
END;

DROP TRIGGER IF EXISTS internet.BankPaymentLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BankPaymentLogUpdate AFTER UPDATE ON internet.BankPayments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.BankPaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		PaymentId = OLD.Id,
		PayedOn = NULLIF(NEW.PayedOn, OLD.PayedOn),
		Sum = NULLIF(NEW.Sum, OLD.Sum),
		PayerId = NULLIF(NEW.PayerId, OLD.PayerId),
		RecipientId = NULLIF(NEW.RecipientId, OLD.RecipientId),
		RegistredOn = NULLIF(NEW.RegistredOn, OLD.RegistredOn),
		Comment = NULLIF(NEW.Comment, OLD.Comment),
		DocumentNumber = NULLIF(NEW.DocumentNumber, OLD.DocumentNumber),
		PayerInn = NULLIF(NEW.PayerInn, OLD.PayerInn),
		PayerName = NULLIF(NEW.PayerName, OLD.PayerName),
		PayerAccountCode = NULLIF(NEW.PayerAccountCode, OLD.PayerAccountCode),
		PayerBankDescription = NULLIF(NEW.PayerBankDescription, OLD.PayerBankDescription),
		PayerBankBic = NULLIF(NEW.PayerBankBic, OLD.PayerBankBic),
		PayerBankAccountCode = NULLIF(NEW.PayerBankAccountCode, OLD.PayerBankAccountCode),
		RecipientInn = NULLIF(NEW.RecipientInn, OLD.RecipientInn),
		RecipientName = NULLIF(NEW.RecipientName, OLD.RecipientName),
		RecipientAccountCode = NULLIF(NEW.RecipientAccountCode, OLD.RecipientAccountCode),
		RecipientBankDescription = NULLIF(NEW.RecipientBankDescription, OLD.RecipientBankDescription),
		RecipientBankBic = NULLIF(NEW.RecipientBankBic, OLD.RecipientBankBic),
		RecipientBankAccountCode = NULLIF(NEW.RecipientBankAccountCode, OLD.RecipientBankAccountCode),
		OperatorComment = NULLIF(NEW.OperatorComment, OLD.OperatorComment),
		ForAd = NULLIF(NEW.ForAd, OLD.ForAd),
		AdSum = NULLIF(NEW.AdSum, OLD.AdSum),
		Ad = NULLIF(NEW.Ad, OLD.Ad);
END;

DROP TRIGGER IF EXISTS internet.BankPaymentLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BankPaymentLogInsert AFTER INSERT ON internet.BankPayments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.BankPaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		PaymentId = NEW.Id,
		PayedOn = NEW.PayedOn,
		Sum = NEW.Sum,
		PayerId = NEW.PayerId,
		RecipientId = NEW.RecipientId,
		RegistredOn = NEW.RegistredOn,
		Comment = NEW.Comment,
		DocumentNumber = NEW.DocumentNumber,
		PayerInn = NEW.PayerInn,
		PayerName = NEW.PayerName,
		PayerAccountCode = NEW.PayerAccountCode,
		PayerBankDescription = NEW.PayerBankDescription,
		PayerBankBic = NEW.PayerBankBic,
		PayerBankAccountCode = NEW.PayerBankAccountCode,
		RecipientInn = NEW.RecipientInn,
		RecipientName = NEW.RecipientName,
		RecipientAccountCode = NEW.RecipientAccountCode,
		RecipientBankDescription = NEW.RecipientBankDescription,
		RecipientBankBic = NEW.RecipientBankBic,
		RecipientBankAccountCode = NEW.RecipientBankAccountCode,
		OperatorComment = NEW.OperatorComment,
		ForAd = NEW.ForAd,
		AdSum = NEW.AdSum,
		Ad = NEW.Ad;
END;

