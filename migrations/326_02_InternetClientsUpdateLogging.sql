
alter table Logs.ClientInternetLogs
add column PostponedPayment datetime,
add column DebtWork tinyint(1) unsigned
;

DROP TRIGGER IF EXISTS Internet.ClientLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientLogDelete AFTER DELETE ON Internet.Clients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ClientId = OLD.Id,
		Disabled = OLD.Disabled,
		Name = OLD.Name,
		Type = OLD.Type,
		PhysicalClient = OLD.PhysicalClient,
		RatedPeriodDate = OLD.RatedPeriodDate,
		DebtDays = OLD.DebtDays,
		ShowBalanceWarningPage = OLD.ShowBalanceWarningPage,
		BeginWork = OLD.BeginWork,
		LawyerPerson = OLD.LawyerPerson,
		RegDate = OLD.RegDate,
		WhoRegistered = OLD.WhoRegistered,
		WhoRegisteredName = OLD.WhoRegisteredName,
		WhoConnected = OLD.WhoConnected,
		WhoConnectedName = OLD.WhoConnectedName,
		ConnectedDate = OLD.ConnectedDate,
		AutoUnblocked = OLD.AutoUnblocked,
		Status = OLD.Status,
		AdditionalStatus = OLD.AdditionalStatus,
		PostponedPayment = OLD.PostponedPayment,
		DebtWork = OLD.DebtWork;
END;

DROP TRIGGER IF EXISTS Internet.ClientLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientLogUpdate AFTER UPDATE ON Internet.Clients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ClientId = OLD.Id,
		Disabled = NULLIF(NEW.Disabled, OLD.Disabled),
		Name = NULLIF(NEW.Name, OLD.Name),
		Type = NULLIF(NEW.Type, OLD.Type),
		PhysicalClient = NULLIF(NEW.PhysicalClient, OLD.PhysicalClient),
		RatedPeriodDate = NULLIF(NEW.RatedPeriodDate, OLD.RatedPeriodDate),
		DebtDays = NULLIF(NEW.DebtDays, OLD.DebtDays),
		ShowBalanceWarningPage = NULLIF(NEW.ShowBalanceWarningPage, OLD.ShowBalanceWarningPage),
		BeginWork = NULLIF(NEW.BeginWork, OLD.BeginWork),
		LawyerPerson = NULLIF(NEW.LawyerPerson, OLD.LawyerPerson),
		RegDate = NULLIF(NEW.RegDate, OLD.RegDate),
		WhoRegistered = NULLIF(NEW.WhoRegistered, OLD.WhoRegistered),
		WhoRegisteredName = NULLIF(NEW.WhoRegisteredName, OLD.WhoRegisteredName),
		WhoConnected = NULLIF(NEW.WhoConnected, OLD.WhoConnected),
		WhoConnectedName = NULLIF(NEW.WhoConnectedName, OLD.WhoConnectedName),
		ConnectedDate = NULLIF(NEW.ConnectedDate, OLD.ConnectedDate),
		AutoUnblocked = NULLIF(NEW.AutoUnblocked, OLD.AutoUnblocked),
		Status = NULLIF(NEW.Status, OLD.Status),
		AdditionalStatus = NULLIF(NEW.AdditionalStatus, OLD.AdditionalStatus),
		PostponedPayment = NULLIF(NEW.PostponedPayment, OLD.PostponedPayment),
		DebtWork = NULLIF(NEW.DebtWork, OLD.DebtWork);
END;

DROP TRIGGER IF EXISTS Internet.ClientLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.ClientLogInsert AFTER INSERT ON Internet.Clients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ClientId = NEW.Id,
		Disabled = NEW.Disabled,
		Name = NEW.Name,
		Type = NEW.Type,
		PhysicalClient = NEW.PhysicalClient,
		RatedPeriodDate = NEW.RatedPeriodDate,
		DebtDays = NEW.DebtDays,
		ShowBalanceWarningPage = NEW.ShowBalanceWarningPage,
		BeginWork = NEW.BeginWork,
		LawyerPerson = NEW.LawyerPerson,
		RegDate = NEW.RegDate,
		WhoRegistered = NEW.WhoRegistered,
		WhoRegisteredName = NEW.WhoRegisteredName,
		WhoConnected = NEW.WhoConnected,
		WhoConnectedName = NEW.WhoConnectedName,
		ConnectedDate = NEW.ConnectedDate,
		AutoUnblocked = NEW.AutoUnblocked,
		Status = NEW.Status,
		AdditionalStatus = NEW.AdditionalStatus,
		PostponedPayment = NEW.PostponedPayment,
		DebtWork = NEW.DebtWork;
END;

