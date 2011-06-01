
CREATE TABLE  `logs`.`LawyerPersonInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `PersonId` int(10) unsigned not null,
  `Tariff` decimal(10,2),
  `Balance` decimal(10,2),
  `FullName` varchar(255),
  `ShortName` varchar(45),
  `LawyerAdress` varchar(255),
  `ActualAdress` varchar(255),
  `INN` varchar(45),
  `Email` varchar(45),
  `Telephone` varchar(45),
  `Speed` int(10) unsigned,
  `WhoRegistered` int(10) unsigned,
  `WhoRegisteredName` varchar(45),
  `WhoConnected` int(10) unsigned,
  `WhoConnectedName` varchar(45),
  `Status` int(10) unsigned,
  `RegDate` datetime,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.LawyerPersonLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.LawyerPersonLogDelete AFTER DELETE ON Internet.LawyerPerson
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.LawyerPersonInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		PersonId = OLD.Id,
		Tariff = OLD.Tariff,
		Balance = OLD.Balance,
		FullName = OLD.FullName,
		ShortName = OLD.ShortName,
		LawyerAdress = OLD.LawyerAdress,
		ActualAdress = OLD.ActualAdress,
		INN = OLD.INN,
		Email = OLD.Email,
		Telephone = OLD.Telephone,
		Speed = OLD.Speed,
		WhoRegistered = OLD.WhoRegistered,
		WhoRegisteredName = OLD.WhoRegisteredName,
		WhoConnected = OLD.WhoConnected,
		WhoConnectedName = OLD.WhoConnectedName,
		Status = OLD.Status,
		RegDate = OLD.RegDate;
END;

DROP TRIGGER IF EXISTS Internet.LawyerPersonLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.LawyerPersonLogUpdate AFTER UPDATE ON Internet.LawyerPerson
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.LawyerPersonInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		PersonId = OLD.Id,
		Tariff = NULLIF(NEW.Tariff, OLD.Tariff),
		Balance = NULLIF(NEW.Balance, OLD.Balance),
		FullName = NULLIF(NEW.FullName, OLD.FullName),
		ShortName = NULLIF(NEW.ShortName, OLD.ShortName),
		LawyerAdress = NULLIF(NEW.LawyerAdress, OLD.LawyerAdress),
		ActualAdress = NULLIF(NEW.ActualAdress, OLD.ActualAdress),
		INN = NULLIF(NEW.INN, OLD.INN),
		Email = NULLIF(NEW.Email, OLD.Email),
		Telephone = NULLIF(NEW.Telephone, OLD.Telephone),
		Speed = NULLIF(NEW.Speed, OLD.Speed),
		WhoRegistered = NULLIF(NEW.WhoRegistered, OLD.WhoRegistered),
		WhoRegisteredName = NULLIF(NEW.WhoRegisteredName, OLD.WhoRegisteredName),
		WhoConnected = NULLIF(NEW.WhoConnected, OLD.WhoConnected),
		WhoConnectedName = NULLIF(NEW.WhoConnectedName, OLD.WhoConnectedName),
		Status = NULLIF(NEW.Status, OLD.Status),
		RegDate = NULLIF(NEW.RegDate, OLD.RegDate);
END;

DROP TRIGGER IF EXISTS Internet.LawyerPersonLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.LawyerPersonLogInsert AFTER INSERT ON Internet.LawyerPerson
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.LawyerPersonInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		PersonId = NEW.Id,
		Tariff = NEW.Tariff,
		Balance = NEW.Balance,
		FullName = NEW.FullName,
		ShortName = NEW.ShortName,
		LawyerAdress = NEW.LawyerAdress,
		ActualAdress = NEW.ActualAdress,
		INN = NEW.INN,
		Email = NEW.Email,
		Telephone = NEW.Telephone,
		Speed = NEW.Speed,
		WhoRegistered = NEW.WhoRegistered,
		WhoRegisteredName = NEW.WhoRegisteredName,
		WhoConnected = NEW.WhoConnected,
		WhoConnectedName = NEW.WhoConnectedName,
		Status = NEW.Status,
		RegDate = NEW.RegDate;
END;

