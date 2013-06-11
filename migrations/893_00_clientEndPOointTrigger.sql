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

DROP TRIGGER IF EXISTS internet.ClientendpointLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.ClientendpointLogUpdate AFTER UPDATE ON internet.clientendpoints
FOR EACH ROW BEGIN
	IF (
	NULLIF(NEW.ActualPackageId, OLD.ActualPackageId) is null and
	(
		NEW.Disabled is not null or
		NEW.Ip is not null or
		NEW.Client is not null or
		NEW.Module is not null or
		NEW.Port is not null or
		NEW.Switch is not null or
		NEW.Monitoring is not null or
		NEW.PackageId is not null or
		NEW.MaxLeaseCount is not null or
		NEW.Pool is not null)
	) THEN
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