
CREATE TABLE  `logs`.`SmsMessageLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `MessageId` int(10) unsigned not null,
  `CreateDate` datetime,
  `SendToOperatorDate` datetime,
  `ShouldBeSend` datetime,
  `Text` varchar(255),
  `PhoneNumber` varchar(255),
  `IsSended` tinyint(1),
  `SMSID` varchar(255),
  `Client` int(10) unsigned,
  `ServerRequest` int(11),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS internet.SmsMessageLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SmsMessageLogDelete AFTER DELETE ON internet.SmsMessages
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SmsMessageLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		MessageId = OLD.Id,
		CreateDate = OLD.CreateDate,
		SendToOperatorDate = OLD.SendToOperatorDate,
		ShouldBeSend = OLD.ShouldBeSend,
		Text = OLD.Text,
		PhoneNumber = OLD.PhoneNumber,
		IsSended = OLD.IsSended,
		SMSID = OLD.SMSID,
		Client = OLD.Client,
		ServerRequest = OLD.ServerRequest;
END;

DROP TRIGGER IF EXISTS internet.SmsMessageLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SmsMessageLogUpdate AFTER UPDATE ON internet.SmsMessages
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SmsMessageLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		MessageId = OLD.Id,
		CreateDate = NULLIF(NEW.CreateDate, OLD.CreateDate),
		SendToOperatorDate = NULLIF(NEW.SendToOperatorDate, OLD.SendToOperatorDate),
		ShouldBeSend = NULLIF(NEW.ShouldBeSend, OLD.ShouldBeSend),
		Text = NULLIF(NEW.Text, OLD.Text),
		PhoneNumber = NULLIF(NEW.PhoneNumber, OLD.PhoneNumber),
		IsSended = NULLIF(NEW.IsSended, OLD.IsSended),
		SMSID = NULLIF(NEW.SMSID, OLD.SMSID),
		Client = NULLIF(NEW.Client, OLD.Client),
		ServerRequest = NULLIF(NEW.ServerRequest, OLD.ServerRequest);
END;

DROP TRIGGER IF EXISTS internet.SmsMessageLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.SmsMessageLogInsert AFTER INSERT ON internet.SmsMessages
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SmsMessageLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		MessageId = NEW.Id,
		CreateDate = NEW.CreateDate,
		SendToOperatorDate = NEW.SendToOperatorDate,
		ShouldBeSend = NEW.ShouldBeSend,
		Text = NEW.Text,
		PhoneNumber = NEW.PhoneNumber,
		IsSended = NEW.IsSended,
		SMSID = NEW.SMSID,
		Client = NEW.Client,
		ServerRequest = NEW.ServerRequest;
END;

