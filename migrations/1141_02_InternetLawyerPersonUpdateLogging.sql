
alter table Logs.LawyerPersonInternetLogs
add column RegionId int(10) unsigned,
add column PeriodEnd datetime
;

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
		Speed = OLD.Speed,
		WhoRegistered = OLD.WhoRegistered,
		WhoRegisteredName = OLD.WhoRegisteredName,
		WhoConnected = OLD.WhoConnected,
		WhoConnectedName = OLD.WhoConnectedName,
		Status = OLD.Status,
		RegDate = OLD.RegDate,
		ContactPerson = OLD.ContactPerson,
		MailingAddress = OLD.MailingAddress,
		RegionId = OLD.RegionId,
		PeriodEnd = OLD.PeriodEnd;
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
		Speed = NULLIF(NEW.Speed, OLD.Speed),
		WhoRegistered = NULLIF(NEW.WhoRegistered, OLD.WhoRegistered),
		WhoRegisteredName = NULLIF(NEW.WhoRegisteredName, OLD.WhoRegisteredName),
		WhoConnected = NULLIF(NEW.WhoConnected, OLD.WhoConnected),
		WhoConnectedName = NULLIF(NEW.WhoConnectedName, OLD.WhoConnectedName),
		Status = NULLIF(NEW.Status, OLD.Status),
		RegDate = NULLIF(NEW.RegDate, OLD.RegDate),
		ContactPerson = NULLIF(NEW.ContactPerson, OLD.ContactPerson),
		MailingAddress = NULLIF(NEW.MailingAddress, OLD.MailingAddress),
		RegionId = NULLIF(NEW.RegionId, OLD.RegionId),
		PeriodEnd = NULLIF(NEW.PeriodEnd, OLD.PeriodEnd);
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
		Speed = NEW.Speed,
		WhoRegistered = NEW.WhoRegistered,
		WhoRegisteredName = NEW.WhoRegisteredName,
		WhoConnected = NEW.WhoConnected,
		WhoConnectedName = NEW.WhoConnectedName,
		Status = NEW.Status,
		RegDate = NEW.RegDate,
		ContactPerson = NEW.ContactPerson,
		MailingAddress = NEW.MailingAddress,
		RegionId = NEW.RegionId,
		PeriodEnd = NEW.PeriodEnd;
END;
