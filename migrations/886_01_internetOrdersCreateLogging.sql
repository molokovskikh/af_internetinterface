
CREATE TABLE  `logs`.`OrderInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `OrderId` int(10) unsigned not null,
  `Number` bigint(20) unsigned,
  `BeginDate` datetime,
  `EndDate` datetime,
  `EndPoint` int(10) unsigned,
  `ClientId` int(10) unsigned,
  `Disabled` tinyint(1) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.OrderLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderLogDelete AFTER DELETE ON internet.Orders
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		OrderId = OLD.Id,
		Number = OLD.Number,
		BeginDate = OLD.BeginDate,
		EndDate = OLD.EndDate,
		EndPoint = OLD.EndPoint,
		ClientId = OLD.ClientId,
		Disabled = OLD.Disabled;
END;

DROP TRIGGER IF EXISTS internet.OrderLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderLogUpdate AFTER UPDATE ON internet.Orders
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		OrderId = OLD.Id,
		Number = NULLIF(NEW.Number, OLD.Number),
		BeginDate = NULLIF(NEW.BeginDate, OLD.BeginDate),
		EndDate = NULLIF(NEW.EndDate, OLD.EndDate),
		EndPoint = NULLIF(NEW.EndPoint, OLD.EndPoint),
		ClientId = NULLIF(NEW.ClientId, OLD.ClientId),
		Disabled = NULLIF(NEW.Disabled, OLD.Disabled);
END;

DROP TRIGGER IF EXISTS internet.OrderLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.OrderLogInsert AFTER INSERT ON internet.Orders
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.OrderInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		OrderId = NEW.Id,
		Number = NEW.Number,
		BeginDate = NEW.BeginDate,
		EndDate = NEW.EndDate,
		EndPoint = NEW.EndPoint,
		ClientId = NEW.ClientId,
		Disabled = NEW.Disabled;
END;