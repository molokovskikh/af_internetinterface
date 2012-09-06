
CREATE TABLE  `logs`.`BookmarkLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `BookmarkId` int(10) unsigned not null,
  `Text` varchar(255),
  `Date` datetime,
  `CreateDate` datetime,
  `Creator` int(10) unsigned,
  `Deleted` tinyint(1),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.BookmarkLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BookmarkLogDelete AFTER DELETE ON internet.Bookmarks
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.BookmarkLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		BookmarkId = OLD.Id,
		Text = OLD.Text,
		Date = OLD.Date,
		CreateDate = OLD.CreateDate,
		Creator = OLD.Creator,
		Deleted = OLD.Deleted;
END;

DROP TRIGGER IF EXISTS internet.BookmarkLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BookmarkLogUpdate AFTER UPDATE ON internet.Bookmarks
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.BookmarkLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		BookmarkId = OLD.Id,
		Text = NULLIF(NEW.Text, OLD.Text),
		Date = NULLIF(NEW.Date, OLD.Date),
		CreateDate = NULLIF(NEW.CreateDate, OLD.CreateDate),
		Creator = NULLIF(NEW.Creator, OLD.Creator),
		Deleted = NULLIF(NEW.Deleted, OLD.Deleted);
END;

DROP TRIGGER IF EXISTS internet.BookmarkLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.BookmarkLogInsert AFTER INSERT ON internet.Bookmarks
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.BookmarkLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		BookmarkId = NEW.Id,
		Text = NEW.Text,
		Date = NEW.Date,
		CreateDate = NEW.CreateDate,
		Creator = NEW.Creator,
		Deleted = NEW.Deleted;
END;

