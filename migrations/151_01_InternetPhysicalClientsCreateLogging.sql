
CREATE TABLE  `logs`.`PhysicalClientInternetLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `ClientId` int(10) unsigned not null,
  `Name` varchar(255),
  `Surname` varchar(255),
  `Patronymic` varchar(255),
  `City` varchar(255),
  `Street` varchar(255),
  `House` varchar(255),
  `CaseHouse` varchar(255),
  `Apartment` varchar(255),
  `Entrance` varchar(255),
  `Floor` varchar(255),
  `PhoneNumber` varchar(255),
  `HomePhoneNumber` varchar(255),
  `WhenceAbout` varchar(255),
  `PassportSeries` varchar(255),
  `PassportNumber` varchar(255),
  `PassportDate` datetime,
  `WhoGivePassport` varchar(255),
  `RegistrationAdress` varchar(255),
  `Balance` decimal(10,2),
  `Email` varchar(255),
  `Password` varchar(255),
  `Tariff` int(10) unsigned,
  `AutoUnblocked` tinyint(1) unsigned,
  `ConnectSum` decimal(10,2),
  `ConnectionPaid` tinyint(1) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Internet.PhysicalClientLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PhysicalClientLogDelete AFTER DELETE ON Internet.PhysicalClients
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
		PhoneNumber = OLD.PhoneNumber,
		HomePhoneNumber = OLD.HomePhoneNumber,
		WhenceAbout = OLD.WhenceAbout,
		PassportSeries = OLD.PassportSeries,
		PassportNumber = OLD.PassportNumber,
		PassportDate = OLD.PassportDate,
		WhoGivePassport = OLD.WhoGivePassport,
		RegistrationAdress = OLD.RegistrationAdress,
		Balance = OLD.Balance,
		Email = OLD.Email,
		Password = OLD.Password,
		Tariff = OLD.Tariff,
		AutoUnblocked = OLD.AutoUnblocked,
		ConnectSum = OLD.ConnectSum,
		ConnectionPaid = OLD.ConnectionPaid;
END;

DROP TRIGGER IF EXISTS Internet.PhysicalClientLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PhysicalClientLogUpdate AFTER UPDATE ON Internet.PhysicalClients
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
		PhoneNumber = NULLIF(NEW.PhoneNumber, OLD.PhoneNumber),
		HomePhoneNumber = NULLIF(NEW.HomePhoneNumber, OLD.HomePhoneNumber),
		WhenceAbout = NULLIF(NEW.WhenceAbout, OLD.WhenceAbout),
		PassportSeries = NULLIF(NEW.PassportSeries, OLD.PassportSeries),
		PassportNumber = NULLIF(NEW.PassportNumber, OLD.PassportNumber),
		PassportDate = NULLIF(NEW.PassportDate, OLD.PassportDate),
		WhoGivePassport = NULLIF(NEW.WhoGivePassport, OLD.WhoGivePassport),
		RegistrationAdress = NULLIF(NEW.RegistrationAdress, OLD.RegistrationAdress),
		Balance = NULLIF(NEW.Balance, OLD.Balance),
		Email = NULLIF(NEW.Email, OLD.Email),
		Password = NULLIF(NEW.Password, OLD.Password),
		Tariff = NULLIF(NEW.Tariff, OLD.Tariff),
		AutoUnblocked = NULLIF(NEW.AutoUnblocked, OLD.AutoUnblocked),
		ConnectSum = NULLIF(NEW.ConnectSum, OLD.ConnectSum),
		ConnectionPaid = NULLIF(NEW.ConnectionPaid, OLD.ConnectionPaid);
END;

DROP TRIGGER IF EXISTS Internet.PhysicalClientLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Internet.PhysicalClientLogInsert AFTER INSERT ON Internet.PhysicalClients
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
		PhoneNumber = NEW.PhoneNumber,
		HomePhoneNumber = NEW.HomePhoneNumber,
		WhenceAbout = NEW.WhenceAbout,
		PassportSeries = NEW.PassportSeries,
		PassportNumber = NEW.PassportNumber,
		PassportDate = NEW.PassportDate,
		WhoGivePassport = NEW.WhoGivePassport,
		RegistrationAdress = NEW.RegistrationAdress,
		Balance = NEW.Balance,
		Email = NEW.Email,
		Password = NEW.Password,
		Tariff = NEW.Tariff,
		AutoUnblocked = NEW.AutoUnblocked,
		ConnectSum = NEW.ConnectSum,
		ConnectionPaid = NEW.ConnectionPaid;
END;

