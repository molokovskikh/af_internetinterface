
CREATE TABLE  `logs`.`WriteOffInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `OffId` int(10) unsigned not null,
  `WriteOffSum` decimal(10,2),
  `WriteOffDate` datetime,
  `Client` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.WriteOffLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.WriteOffLogDelete AFTER DELETE ON Internet.WriteOff
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.WriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		OffId = OLD.Id,
		WriteOffSum = OLD.WriteOffSum,
		WriteOffDate = OLD.WriteOffDate,
		Client = OLD.Client;
END;

DROP TRIGGER IF EXISTS Internet.WriteOffLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.WriteOffLogUpdate AFTER UPDATE ON Internet.WriteOff
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.WriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		OffId = OLD.Id,
		WriteOffSum = NULLIF(NEW.WriteOffSum, OLD.WriteOffSum),
		WriteOffDate = NULLIF(NEW.WriteOffDate, OLD.WriteOffDate),
		Client = NULLIF(NEW.Client, OLD.Client);
END;

DROP TRIGGER IF EXISTS Internet.WriteOffLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.WriteOffLogInsert AFTER INSERT ON Internet.WriteOff
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.WriteOffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		OffId = NEW.Id,
		WriteOffSum = NEW.WriteOffSum,
		WriteOffDate = NEW.WriteOffDate,
		Client = NEW.Client;
END;

