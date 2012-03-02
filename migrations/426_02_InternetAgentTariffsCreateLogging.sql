
CREATE TABLE  `logs`.`AgentTariffInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `TariffId` int(10) unsigned not null,
  `ActionName` varchar(45),
  `Sum` decimal(10,2),
  `Description` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.AgentTariffLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.AgentTariffLogDelete AFTER DELETE ON Internet.AgentTariffs
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.AgentTariffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		TariffId = OLD.Id,
		ActionName = OLD.ActionName,
		Sum = OLD.Sum,
		Description = OLD.Description;
END;

DROP TRIGGER IF EXISTS Internet.AgentTariffLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.AgentTariffLogUpdate AFTER UPDATE ON Internet.AgentTariffs
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.AgentTariffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		TariffId = OLD.Id,
		ActionName = NULLIF(NEW.ActionName, OLD.ActionName),
		Sum = NULLIF(NEW.Sum, OLD.Sum),
		Description = NULLIF(NEW.Description, OLD.Description);
END;

DROP TRIGGER IF EXISTS Internet.AgentTariffLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.AgentTariffLogInsert AFTER INSERT ON Internet.AgentTariffs
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.AgentTariffInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		TariffId = NEW.Id,
		ActionName = NEW.ActionName,
		Sum = NEW.Sum,
		Description = NEW.Description;
END;

