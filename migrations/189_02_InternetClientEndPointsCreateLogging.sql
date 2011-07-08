
CREATE TABLE  `logs`.`ClientEndPointInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `PointId` int(10) unsigned not null,
  `Disabled` tinyint(1),
  `Ip` int(10) unsigned,
  `Client` int(10) unsigned,
  `Module` tinyint(3) unsigned,
  `Port` tinyint(3) unsigned,
  `Switch` int(10) unsigned,
  `Monitoring` tinyint(1),
  `PackageId` int(11),
  `MaxLeaseCount` int(11),
  `Pool` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.ClientEndPointLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientEndPointLogDelete AFTER DELETE ON Internet.ClientEndPoints
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientEndPointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		PointId = OLD.Id,
		Disabled = OLD.Disabled,
		Ip = OLD.Ip,
		Client = OLD.Client,
		Module = OLD.Module,
		Port = OLD.Port,
		Switch = OLD.Switch,
		Monitoring = OLD.Monitoring,
		PackageId = OLD.PackageId,
		MaxLeaseCount = OLD.MaxLeaseCount,
		Pool = OLD.Pool;
END;

DROP TRIGGER IF EXISTS Internet.ClientEndPointLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientEndPointLogUpdate AFTER UPDATE ON Internet.ClientEndPoints
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientEndPointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		PointId = OLD.Id,
		Disabled = NULLIF(NEW.Disabled, OLD.Disabled),
		Ip = NULLIF(NEW.Ip, OLD.Ip),
		Client = NULLIF(NEW.Client, OLD.Client),
		Module = NULLIF(NEW.Module, OLD.Module),
		Port = NULLIF(NEW.Port, OLD.Port),
		Switch = NULLIF(NEW.Switch, OLD.Switch),
		Monitoring = NULLIF(NEW.Monitoring, OLD.Monitoring),
		PackageId = NULLIF(NEW.PackageId, OLD.PackageId),
		MaxLeaseCount = NULLIF(NEW.MaxLeaseCount, OLD.MaxLeaseCount),
		Pool = NULLIF(NEW.Pool, OLD.Pool);
END;

DROP TRIGGER IF EXISTS Internet.ClientEndPointLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientEndPointLogInsert AFTER INSERT ON Internet.ClientEndPoints
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientEndPointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		PointId = NEW.Id,
		Disabled = NEW.Disabled,
		Ip = NEW.Ip,
		Client = NEW.Client,
		Module = NEW.Module,
		Port = NEW.Port,
		Switch = NEW.Switch,
		Monitoring = NEW.Monitoring,
		PackageId = NEW.PackageId,
		MaxLeaseCount = NEW.MaxLeaseCount,
		Pool = NEW.Pool;
END;

