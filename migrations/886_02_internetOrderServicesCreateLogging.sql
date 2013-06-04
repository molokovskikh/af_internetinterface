
CREATE TABLE  `logs`.`OrderServiceInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `ServiceId` int(10) unsigned not null,
  `Description` varchar(255),
  `Cost` decimal(10,0),
  `IsPeriodic` tinyint(1) unsigned,
  `OrderId` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.OrderServiceLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderServiceLogDelete AFTER DELETE ON internet.OrderServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ServiceId = OLD.Id,
		Description = OLD.Description,
		Cost = OLD.Cost,
		IsPeriodic = OLD.IsPeriodic,
		OrderId = OLD.OrderId;
END;

DROP TRIGGER IF EXISTS internet.OrderServiceLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderServiceLogUpdate AFTER UPDATE ON internet.OrderServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ServiceId = OLD.Id,
		Description = NULLIF(NEW.Description, OLD.Description),
		Cost = NULLIF(NEW.Cost, OLD.Cost),
		IsPeriodic = NULLIF(NEW.IsPeriodic, OLD.IsPeriodic),
		OrderId = NULLIF(NEW.OrderId, OLD.OrderId);
END;

DROP TRIGGER IF EXISTS internet.OrderServiceLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderServiceLogInsert AFTER INSERT ON internet.OrderServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ServiceId = NEW.Id,
		Description = NEW.Description,
		Cost = NEW.Cost,
		IsPeriodic = NEW.IsPeriodic,
		OrderId = NEW.OrderId;
END;