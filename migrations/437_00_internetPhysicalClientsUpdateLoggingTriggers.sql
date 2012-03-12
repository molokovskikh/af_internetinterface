DROP TRIGGER IF EXISTS internet.PhysicalClientLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.PhysicalClientLogDelete AFTER DELETE ON internet.PhysicalClients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PhysicalClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ClientId = OLD.Id,
		Name = OLD.Name,
		Surname = OLD.Surname,
		Patronymic = OLD.Patronymic,
		City = OLD.City,
		Street = OLD.Street,
		House = OLD.House,
		CaseHouse = OLD.CaseHouse,
		Apartment = OLD.Apartment,
		Entrance = OLD.Entrance,
		Floor = OLD.Floor,
		PassportSeries = OLD.PassportSeries,
		PassportNumber = OLD.PassportNumber,
		PassportDate = OLD.PassportDate,
		WhoGivePassport = OLD.WhoGivePassport,
		RegistrationAdress = OLD.RegistrationAdress,
		Balance = OLD.Balance,
		Password = OLD.Password,
		Tariff = OLD.Tariff,
		AutoUnblocked = OLD.AutoUnblocked,
		ConnectSum = OLD.ConnectSum,
		ConnectionPaid = OLD.ConnectionPaid,
		HouseObj = OLD.HouseObj,
		DateOfBirth = OLD.DateOfBirth,
		Request = OLD.Request;
END;

DROP TRIGGER IF EXISTS internet.PhysicalClientLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.PhysicalClientLogUpdate AFTER UPDATE ON internet.PhysicalClients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PhysicalClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ClientId = OLD.Id,
		Name = NULLIF(NEW.Name, OLD.Name),
		Surname = NULLIF(NEW.Surname, OLD.Surname),
		Patronymic = NULLIF(NEW.Patronymic, OLD.Patronymic),
		City = NULLIF(NEW.City, OLD.City),
		Street = NULLIF(NEW.Street, OLD.Street),
		House = NULLIF(NEW.House, OLD.House),
		CaseHouse = NULLIF(NEW.CaseHouse, OLD.CaseHouse),
		Apartment = NULLIF(NEW.Apartment, OLD.Apartment),
		Entrance = NULLIF(NEW.Entrance, OLD.Entrance),
		Floor = NULLIF(NEW.Floor, OLD.Floor),
		PassportSeries = NULLIF(NEW.PassportSeries, OLD.PassportSeries),
		PassportNumber = NULLIF(NEW.PassportNumber, OLD.PassportNumber),
		PassportDate = NULLIF(NEW.PassportDate, OLD.PassportDate),
		WhoGivePassport = NULLIF(NEW.WhoGivePassport, OLD.WhoGivePassport),
		RegistrationAdress = NULLIF(NEW.RegistrationAdress, OLD.RegistrationAdress),
		Balance = NULLIF(NEW.Balance, OLD.Balance),
		Password = NULLIF(NEW.Password, OLD.Password),
		Tariff = NULLIF(NEW.Tariff, OLD.Tariff),
		AutoUnblocked = NULLIF(NEW.AutoUnblocked, OLD.AutoUnblocked),
		ConnectSum = NULLIF(NEW.ConnectSum, OLD.ConnectSum),
		ConnectionPaid = NULLIF(NEW.ConnectionPaid, OLD.ConnectionPaid),
		HouseObj = NULLIF(NEW.HouseObj, OLD.HouseObj),
		DateOfBirth = NULLIF(NEW.DateOfBirth, OLD.DateOfBirth),
		Request = NULLIF(NEW.Request, OLD.Request);
END;

DROP TRIGGER IF EXISTS internet.PhysicalClientLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER internet.PhysicalClientLogInsert AFTER INSERT ON internet.PhysicalClients
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.PhysicalClientInternetLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ClientId = NEW.Id,
		Name = NEW.Name,
		Surname = NEW.Surname,
		Patronymic = NEW.Patronymic,
		City = NEW.City,
		Street = NEW.Street,
		House = NEW.House,
		CaseHouse = NEW.CaseHouse,
		Apartment = NEW.Apartment,
		Entrance = NEW.Entrance,
		Floor = NEW.Floor,
		PassportSeries = NEW.PassportSeries,
		PassportNumber = NEW.PassportNumber,
		PassportDate = NEW.PassportDate,
		WhoGivePassport = NEW.WhoGivePassport,
		RegistrationAdress = NEW.RegistrationAdress,
		Balance = NEW.Balance,
		Password = NEW.Password,
		Tariff = NEW.Tariff,
		AutoUnblocked = NEW.AutoUnblocked,
		ConnectSum = NEW.ConnectSum,
		ConnectionPaid = NEW.ConnectionPaid,
		HouseObj = NEW.HouseObj,
		DateOfBirth = NEW.DateOfBirth,
		Request = NEW.Request;
END;

