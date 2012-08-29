
CREATE TABLE  `logs`.`InternetsettingInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `InternetsettingId` int(10) unsigned not null,
  `NextBillingDate` datetime,
  `NextStaticIpDate` datetime,
  `NextSmsSendDate` datetime,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.InternetsettingLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.InternetsettingLogDelete AFTER DELETE ON internet.internetsettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.InternetsettingInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		InternetsettingId = OLD.Id,
		NextBillingDate = OLD.NextBillingDate,
		NextStaticIpDate = OLD.NextStaticIpDate,
		NextSmsSendDate = OLD.NextSmsSendDate;
END;

DROP TRIGGER IF EXISTS internet.InternetsettingLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.InternetsettingLogUpdate AFTER UPDATE ON internet.internetsettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.InternetsettingInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		InternetsettingId = OLD.Id,
		NextBillingDate = NULLIF(NEW.NextBillingDate, OLD.NextBillingDate),
		NextStaticIpDate = NULLIF(NEW.NextStaticIpDate, OLD.NextStaticIpDate),
		NextSmsSendDate = NULLIF(NEW.NextSmsSendDate, OLD.NextSmsSendDate);
END;

DROP TRIGGER IF EXISTS internet.InternetsettingLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.InternetsettingLogInsert AFTER INSERT ON internet.internetsettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.InternetsettingInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		InternetsettingId = NEW.Id,
		NextBillingDate = NEW.NextBillingDate,
		NextStaticIpDate = NEW.NextStaticIpDate,
		NextSmsSendDate = NEW.NextSmsSendDate;
END;

