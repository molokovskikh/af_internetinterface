CREATE TABLE  `logs`.`SiteContentInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `ContentId` int(10) unsigned not null,
  `Content` longtext,
  `ViewName` varchar(45),
  `ContentType` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS Internet.SiteContentLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SiteContentLogDelete AFTER DELETE ON Internet.SiteContent
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SiteContentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ContentId = OLD.Id,
		Content = OLD.Content,
		ViewName = OLD.ViewName,
		ContentType = OLD.ContentType;
END;
DROP TRIGGER IF EXISTS Internet.SiteContentLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SiteContentLogUpdate AFTER UPDATE ON Internet.SiteContent
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SiteContentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ContentId = OLD.Id,
		Content = NULLIF(NEW.Content, OLD.Content),
		ViewName = NULLIF(NEW.ViewName, OLD.ViewName),
		ContentType = NULLIF(NEW.ContentType, OLD.ContentType);
END;
DROP TRIGGER IF EXISTS Internet.SiteContentLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.SiteContentLogInsert AFTER INSERT ON Internet.SiteContent
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SiteContentInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ContentId = NEW.Id,
		Content = NEW.Content,
		ViewName = NEW.ViewName,
		ContentType = NEW.ContentType;
END;
