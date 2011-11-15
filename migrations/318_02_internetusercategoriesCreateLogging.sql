
CREATE TABLE  `logs`.`UsercategoryLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `UsercategoryId` int(10) unsigned not null,
  `Comment` varchar(45),
  `ReductionName` varchar(45),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.UsercategoryLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UsercategoryLogDelete AFTER DELETE ON internet.usercategories
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.UsercategoryLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		UsercategoryId = OLD.Id,
		Comment = OLD.Comment,
		ReductionName = OLD.ReductionName;
END;

DROP TRIGGER IF EXISTS internet.UsercategoryLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UsercategoryLogUpdate AFTER UPDATE ON internet.usercategories
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.UsercategoryLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		UsercategoryId = OLD.Id,
		Comment = NULLIF(NEW.Comment, OLD.Comment),
		ReductionName = NULLIF(NEW.ReductionName, OLD.ReductionName);
END;

DROP TRIGGER IF EXISTS internet.UsercategoryLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.UsercategoryLogInsert AFTER INSERT ON internet.usercategories
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.UsercategoryLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		UsercategoryId = NEW.Id,
		Comment = NEW.Comment,
		ReductionName = NEW.ReductionName;
END;

