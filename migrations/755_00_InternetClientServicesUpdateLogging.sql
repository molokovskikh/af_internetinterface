
alter table Logs.ClientServiceInternetLogs
add column ActivatedByUser tinyint(1)
;

DROP TRIGGER IF EXISTS Internet.ClientServiceLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientServiceLogDelete AFTER DELETE ON Internet.ClientServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ClientServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ServiceId = OLD.Id,
		Client = OLD.Client,
		Service = OLD.Service,
		BeginWorkDate = OLD.BeginWorkDate,
		EndWorkDate = OLD.EndWorkDate,
		Activated = OLD.Activated,
		Activator = OLD.Activator,
		Diactivated = OLD.Diactivated,
		ActivatedByUser = OLD.ActivatedByUser;
END;

DROP TRIGGER IF EXISTS Internet.ClientServiceLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientServiceLogUpdate AFTER UPDATE ON Internet.ClientServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ClientServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ServiceId = OLD.Id,
		Client = NULLIF(NEW.Client, OLD.Client),
		Service = NULLIF(NEW.Service, OLD.Service),
		BeginWorkDate = NULLIF(NEW.BeginWorkDate, OLD.BeginWorkDate),
		EndWorkDate = NULLIF(NEW.EndWorkDate, OLD.EndWorkDate),
		Activated = NULLIF(NEW.Activated, OLD.Activated),
		Activator = NULLIF(NEW.Activator, OLD.Activator),
		Diactivated = NULLIF(NEW.Diactivated, OLD.Diactivated),
		ActivatedByUser = NULLIF(NEW.ActivatedByUser, OLD.ActivatedByUser);
END;

DROP TRIGGER IF EXISTS Internet.ClientServiceLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientServiceLogInsert AFTER INSERT ON Internet.ClientServices
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ClientServiceInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ServiceId = NEW.Id,
		Client = NEW.Client,
		Service = NEW.Service,
		BeginWorkDate = NEW.BeginWorkDate,
		EndWorkDate = NEW.EndWorkDate,
		Activated = NEW.Activated,
		Activator = NEW.Activator,
		Diactivated = NEW.Diactivated,
		ActivatedByUser = NEW.ActivatedByUser;
END;

