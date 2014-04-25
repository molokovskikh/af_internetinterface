CREATE TABLE  `logs`.`SubMenuFieldInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `FieldId` int(10) unsigned not null,
  `MenuField` int(10) unsigned,
  `Name` varchar(45),
  `Link` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS Internet.SubMenuFieldLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SubMenuFieldLogDelete AFTER DELETE ON Internet.SubMenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SubMenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		FieldId = OLD.Id,
		MenuField = OLD.MenuField,
		Name = OLD.Name,
		Link = OLD.Link;
END;
DROP TRIGGER IF EXISTS Internet.SubMenuFieldLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SubMenuFieldLogUpdate AFTER UPDATE ON Internet.SubMenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SubMenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		FieldId = OLD.Id,
		MenuField = NULLIF(NEW.MenuField, OLD.MenuField),
		Name = NULLIF(NEW.Name, OLD.Name),
		Link = NULLIF(NEW.Link, OLD.Link);
END;
DROP TRIGGER IF EXISTS Internet.SubMenuFieldLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SubMenuFieldLogInsert AFTER INSERT ON Internet.SubMenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SubMenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		FieldId = NEW.Id,
		MenuField = NEW.MenuField,
		Name = NEW.Name,
		Link = NEW.Link;
END;
