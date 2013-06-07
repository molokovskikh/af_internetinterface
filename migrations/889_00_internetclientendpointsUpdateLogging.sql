delete from `logs`.ClientEndPointInternetLogs
where
Disabled is null and
Ip is null and
Client is null and
Module is null and
Port is null and
Switch is null and
Monitoring is null and
PackageId is null and
MaxLeaseCount is null and
Pool is null;

ALTER TABLE `logs`.`clientendpointinternetlogs` CHANGE COLUMN `PointId` `ClientendpointId` INT(10) UNSIGNED NOT NULL;

alter table Logs.ClientendpointInternetLogs
add column ActualPackageId int(11),
add column WhoConnected int(10) unsigned
;

DROP TRIGGER IF EXISTS internet.ClientendpointLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.ClientendpointLogDelete AFTER DELETE ON internet.clientendpoints
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ClientendpointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ClientendpointId = OLD.Id,
		Disabled = OLD.Disabled,
		Ip = OLD.Ip,
		Client = OLD.Client,
		Module = OLD.Module,
		Port = OLD.Port,
		Switch = OLD.Switch,
		Monitoring = OLD.Monitoring,
		PackageId = OLD.PackageId,
		MaxLeaseCount = OLD.MaxLeaseCount,
		Pool = OLD.Pool,
		ActualPackageId = OLD.ActualPackageId,
		WhoConnected = OLD.WhoConnected;
END;

DROP TRIGGER IF EXISTS internet.ClientendpointLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.ClientendpointLogUpdate AFTER UPDATE ON internet.clientendpoints
FOR EACH ROW BEGIN
	IF (NULLIF(NEW.ActualPackageId, OLD.ActualPackageId) is null) THEN
	INSERT
	INTO `logs`.ClientendpointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ClientendpointId = OLD.Id,
		Disabled = NULLIF(NEW.Disabled, OLD.Disabled),
		Ip = NULLIF(NEW.Ip, OLD.Ip),
		Client = NULLIF(NEW.Client, OLD.Client),
		Module = NULLIF(NEW.Module, OLD.Module),
		Port = NULLIF(NEW.Port, OLD.Port),
		Switch = NULLIF(NEW.Switch, OLD.Switch),
		Monitoring = NULLIF(NEW.Monitoring, OLD.Monitoring),
		PackageId = NULLIF(NEW.PackageId, OLD.PackageId),
		MaxLeaseCount = NULLIF(NEW.MaxLeaseCount, OLD.MaxLeaseCount),
		Pool = NULLIF(NEW.Pool, OLD.Pool),
		ActualPackageId = NULLIF(NEW.ActualPackageId, OLD.ActualPackageId),
		WhoConnected = NULLIF(NEW.WhoConnected, OLD.WhoConnected);
	END IF;
END;

DROP TRIGGER IF EXISTS internet.ClientendpointLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.ClientendpointLogInsert AFTER INSERT ON internet.clientendpoints
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ClientendpointInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ClientendpointId = NEW.Id,
		Disabled = NEW.Disabled,
		Ip = NEW.Ip,
		Client = NEW.Client,
		Module = NEW.Module,
		Port = NEW.Port,
		Switch = NEW.Switch,
		Monitoring = NEW.Monitoring,
		PackageId = NEW.PackageId,
		MaxLeaseCount = NEW.MaxLeaseCount,
		Pool = NEW.Pool,
		ActualPackageId = NEW.ActualPackageId,
		WhoConnected = NEW.WhoConnected;
END;