
CREATE TABLE  `logs`.`AgentInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `AgentId` int(10) unsigned not null,
  `Name` varchar(45),
  `Partner` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.AgentLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.AgentLogDelete AFTER DELETE ON internet.Agents
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.AgentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		AgentId = OLD.Id,
		Name = OLD.Name,
		Partner = OLD.Partner;
END;

DROP TRIGGER IF EXISTS internet.AgentLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.AgentLogUpdate AFTER UPDATE ON internet.Agents
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.AgentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		AgentId = OLD.Id,
		Name = NULLIF(NEW.Name, OLD.Name),
		Partner = NULLIF(NEW.Partner, OLD.Partner);
END;

DROP TRIGGER IF EXISTS internet.AgentLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.AgentLogInsert AFTER INSERT ON internet.Agents
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.AgentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		AgentId = NEW.Id,
		Name = NEW.Name,
		Partner = NEW.Partner;
END;