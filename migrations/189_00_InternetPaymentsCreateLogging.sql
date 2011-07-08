
CREATE TABLE  `logs`.`PaymentInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `PaymentId` int(10) unsigned not null,
  `RecievedOn` datetime,
  `PaidOn` datetime,
  `TransactionId` varchar(255),
  `Sum` decimal(19,5),
  `Client` int(10) unsigned,
  `Agent` int(10) unsigned,
  `BillingAccount` tinyint(1) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.PaymentLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PaymentLogDelete AFTER DELETE ON Internet.Payments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		PaymentId = OLD.Id,
		RecievedOn = OLD.RecievedOn,
		PaidOn = OLD.PaidOn,
		TransactionId = OLD.TransactionId,
		Sum = OLD.Sum,
		Client = OLD.Client,
		Agent = OLD.Agent,
		BillingAccount = OLD.BillingAccount;
END;

DROP TRIGGER IF EXISTS Internet.PaymentLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PaymentLogUpdate AFTER UPDATE ON Internet.Payments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		PaymentId = OLD.Id,
		RecievedOn = NULLIF(NEW.RecievedOn, OLD.RecievedOn),
		PaidOn = NULLIF(NEW.PaidOn, OLD.PaidOn),
		TransactionId = NULLIF(NEW.TransactionId, OLD.TransactionId),
		Sum = NULLIF(NEW.Sum, OLD.Sum),
		Client = NULLIF(NEW.Client, OLD.Client),
		Agent = NULLIF(NEW.Agent, OLD.Agent),
		BillingAccount = NULLIF(NEW.BillingAccount, OLD.BillingAccount);
END;

DROP TRIGGER IF EXISTS Internet.PaymentLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PaymentLogInsert AFTER INSERT ON Internet.Payments
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PaymentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		PaymentId = NEW.Id,
		RecievedOn = NEW.RecievedOn,
		PaidOn = NEW.PaidOn,
		TransactionId = NEW.TransactionId,
		Sum = NEW.Sum,
		Client = NEW.Client,
		Agent = NEW.Agent,
		BillingAccount = NEW.BillingAccount;
END;

