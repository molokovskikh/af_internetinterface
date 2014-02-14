
alter table Logs.OrderInternetLogs
add column IsActivated tinyint(1),
add column IsDeactivated tinyint(1)
;

DROP TRIGGER IF EXISTS Internet.OrderLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.OrderLogDelete AFTER DELETE ON Internet.Orders
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
		Disabled = OLD.Disabled,
		IsActivated = OLD.IsActivated,
		IsDeactivated = OLD.IsDeactivated;
END;

DROP TRIGGER IF EXISTS Internet.OrderLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.OrderLogUpdate AFTER UPDATE ON Internet.Orders
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
		Disabled = NULLIF(NEW.Disabled, OLD.Disabled),
		IsActivated = NULLIF(NEW.IsActivated, OLD.IsActivated),
		IsDeactivated = NULLIF(NEW.IsDeactivated, OLD.IsDeactivated);
END;

DROP TRIGGER IF EXISTS Internet.OrderLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.OrderLogInsert AFTER INSERT ON Internet.Orders
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
		Disabled = NEW.Disabled,
		IsActivated = NEW.IsActivated,
		IsDeactivated = NEW.IsDeactivated;
END;
