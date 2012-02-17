
CREATE TABLE  `logs`.`SaleSettingLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `SettingId` int(10) unsigned not null,
  `PeriodCount` int(11),
  `MinSale` int(11),
  `MaxSale` int(11),
  `SaleStep` decimal(19,5),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.SaleSettingLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SaleSettingLogDelete AFTER DELETE ON internet.SaleSettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SaleSettingLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		SettingId = OLD.Id,
		PeriodCount = OLD.PeriodCount,
		MinSale = OLD.MinSale,
		MaxSale = OLD.MaxSale,
		SaleStep = OLD.SaleStep;
END;

DROP TRIGGER IF EXISTS internet.SaleSettingLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SaleSettingLogUpdate AFTER UPDATE ON internet.SaleSettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SaleSettingLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		SettingId = OLD.Id,
		PeriodCount = NULLIF(NEW.PeriodCount, OLD.PeriodCount),
		MinSale = NULLIF(NEW.MinSale, OLD.MinSale),
		MaxSale = NULLIF(NEW.MaxSale, OLD.MaxSale),
		SaleStep = NULLIF(NEW.SaleStep, OLD.SaleStep);
END;

DROP TRIGGER IF EXISTS internet.SaleSettingLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SaleSettingLogInsert AFTER INSERT ON internet.SaleSettings
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SaleSettingLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		SettingId = NEW.Id,
		PeriodCount = NEW.PeriodCount,
		MinSale = NEW.MinSale,
		MaxSale = NEW.MaxSale,
		SaleStep = NEW.SaleStep;
END;

