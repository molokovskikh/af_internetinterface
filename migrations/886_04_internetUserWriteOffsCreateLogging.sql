
CREATE TABLE  `logs`.`UserWriteOffInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `OffId` int(10) unsigned not null,
  `Sum` decimal(10,2),
  `Date` datetime,
  `Client` int(10) unsigned,
  `BillingAccount` tinyint(1) unsigned,
  `Comment` varchar(255),
  `Registrator` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.UserWriteOffLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UserWriteOffLogDelete AFTER DELETE ON internet.UserWriteOffs
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.UserWriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		OffId = OLD.Id,
		Sum = OLD.Sum,
		Date = OLD.Date,
		Client = OLD.Client,
		BillingAccount = OLD.BillingAccount,
		Comment = OLD.Comment,
		Registrator = OLD.Registrator;
END;

DROP TRIGGER IF EXISTS internet.UserWriteOffLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UserWriteOffLogUpdate AFTER UPDATE ON internet.UserWriteOffs
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.UserWriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		OffId = OLD.Id,
		Sum = NULLIF(NEW.Sum, OLD.Sum),
		Date = NULLIF(NEW.Date, OLD.Date),
		Client = NULLIF(NEW.Client, OLD.Client),
		BillingAccount = NULLIF(NEW.BillingAccount, OLD.BillingAccount),
		Comment = NULLIF(NEW.Comment, OLD.Comment),
		Registrator = NULLIF(NEW.Registrator, OLD.Registrator);
END;

DROP TRIGGER IF EXISTS internet.UserWriteOffLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UserWriteOffLogInsert AFTER INSERT ON internet.UserWriteOffs
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.UserWriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		OffId = NEW.Id,
		Sum = NEW.Sum,
		Date = NEW.Date,
		Client = NEW.Client,
		BillingAccount = NEW.BillingAccount,
		Comment = NEW.Comment,
		Registrator = NEW.Registrator;
END;