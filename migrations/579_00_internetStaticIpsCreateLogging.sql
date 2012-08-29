CREATE TABLE  `logs`.`StaticIpLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `IpId` int(10) unsigned not null,
  `EndPoint` int(10) unsigned,
  `Ip` varchar(45),
  `Mask` int(10) unsigned,
  `Client` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS internet.StaticIpLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.StaticIpLogDelete AFTER DELETE ON internet.StaticIps
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.StaticIpLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		IpId = OLD.Id,
		EndPoint = OLD.EndPoint,
		Ip = OLD.Ip,
		Mask = OLD.Mask,
		Client = OLD.Client;
END;
DROP TRIGGER IF EXISTS internet.StaticIpLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.StaticIpLogUpdate AFTER UPDATE ON internet.StaticIps
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.StaticIpLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		IpId = OLD.Id,
		EndPoint = NULLIF(NEW.EndPoint, OLD.EndPoint),
		Ip = NULLIF(NEW.Ip, OLD.Ip),
		Mask = NULLIF(NEW.Mask, OLD.Mask),
		Client = NULLIF(NEW.Client, OLD.Client);
END;
DROP TRIGGER IF EXISTS internet.StaticIpLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.StaticIpLogInsert AFTER INSERT ON internet.StaticIps
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.StaticIpLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		IpId = NEW.Id,
		EndPoint = NEW.EndPoint,
		Ip = NEW.Ip,
		Mask = NEW.Mask,
		Client = NEW.Client;
END;