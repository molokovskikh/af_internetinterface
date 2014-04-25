CREATE TABLE  `logs`.`MenuFieldInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `FieldId` int(10) unsigned not null,
  `Name` varchar(45),
  `Link` varchar(255),
  `Hint` varchar(45),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS Internet.MenuFieldLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.MenuFieldLogDelete AFTER DELETE ON Internet.MenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.MenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		FieldId = OLD.Id,
		Name = OLD.Name,
		Link = OLD.Link,
		Hint = OLD.Hint;
END;
DROP TRIGGER IF EXISTS Internet.MenuFieldLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.MenuFieldLogUpdate AFTER UPDATE ON Internet.MenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.MenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		FieldId = OLD.Id,
		Name = NULLIF(NEW.Name, OLD.Name),
		Link = NULLIF(NEW.Link, OLD.Link),
		Hint = NULLIF(NEW.Hint, OLD.Hint);
END;
DROP TRIGGER IF EXISTS Internet.MenuFieldLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.MenuFieldLogInsert AFTER INSERT ON Internet.MenuField
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.MenuFieldInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		FieldId = NEW.Id,
		Name = NEW.Name,
		Link = NEW.Link,
		Hint = NEW.Hint;
END;
