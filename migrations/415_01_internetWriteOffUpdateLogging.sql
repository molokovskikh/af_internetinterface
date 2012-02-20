
alter table Logs.WriteOffInternetLogs
add column Sale decimal(19,5),
add column Comment varchar(255)
;

DROP TRIGGER IF EXISTS internet.WriteOffLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.WriteOffLogDelete AFTER DELETE ON internet.WriteOff
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
		Client = OLD.Client,
		Sale = OLD.Sale,
		Comment = OLD.Comment;
END;

DROP TRIGGER IF EXISTS internet.WriteOffLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.WriteOffLogUpdate AFTER UPDATE ON internet.WriteOff
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
		Client = NULLIF(NEW.Client, OLD.Client),
		Sale = NULLIF(NEW.Sale, OLD.Sale),
		Comment = NULLIF(NEW.Comment, OLD.Comment);
END;

DROP TRIGGER IF EXISTS internet.WriteOffLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.WriteOffLogInsert AFTER INSERT ON internet.WriteOff
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
		Client = NEW.Client,
		Sale = NEW.Sale,
		Comment = NEW.Comment;
END;

