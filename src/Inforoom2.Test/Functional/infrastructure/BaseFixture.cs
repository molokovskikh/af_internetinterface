using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using Billing;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NUnit.Framework;
using Test.Support.Selenium;
using Cookie = OpenQA.Selenium.Cookie;
using Environment = System.Environment;
using SceHelper = Inforoom2.Helpers.SceHelper;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class BaseFixture : SeleniumFixture
	{
		protected ISession DbSession;
		protected string DefaultClientPasword;
		protected string DefaultIpString = "105.168.0.1";
		protected int EndpointIpCounter;
		

		[SetUp]
		public override void IntegrationSetup()
		{
			//Ставим куки, чтобы не отображался popup
			DbSession = MvcApplication.SessionFactory.OpenSession();
			DefaultClientPasword = CryptoPass.GetHashString("password");
			SetCookie("userCity", "Белгород");
			GenerateObjects();
		}

		[TearDown]
		public override void IntegrationTearDown()
		{
			DbSession.Close();
		}

		protected bool IsTextExists(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			return body.Contains(text);
		}

		public void SetCookie(string name, string value)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
			var text = System.Convert.ToBase64String(plainTextBytes);
			var cookie = new Cookie(name, text);
			browser.Manage().Cookies.AddCookie(cookie);
		}
	
		protected string GetCookie(string cookieName)
		{
			var cookie = browser.Manage().Cookies.GetCookieNamed(cookieName);
			if (cookie == null) {
				return string.Empty;
			}

			var base64EncodedBytes = System.Convert.FromBase64String(cookie.Value);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
			
		}
		/// <summary>
		/// Очистка базы данных для тестов
		/// </summary>
		public void CleanDb()
		{
			var str = ConfigurationManager.AppSettings["nhibernateConnectionString"];
			if (string.IsNullOrEmpty(str) || str.Contains("analit.net"))
				throw new Exception("Нельзя проводить тесты на реальных базах данных analit.net");

			var strategy = new TableNamingStrategy();
			var tables = new List<string>();

			//Приоритет удаления данных
			var order = "lawyerperson,regions";
			var parts = order.Split(',');
			foreach (var part in parts)
			{
				var tablename = strategy.TableName(part);
				tables.Add(tablename);
			}

			////Собираем остальные таблицы при помощи моделей проекта
			var types = Assembly.GetAssembly(typeof(BaseModel)).GetTypes().ToList();
			foreach (var t in types)
			{
				var attribute = Attribute.GetCustomAttribute(t, typeof(ClassAttribute)) as ClassAttribute;
				if (attribute != null)
				{
					var name = strategy.TableName(attribute.Table);
					tables.Add(name.ToLower());
				}
			}

			//Удаляем из списка таблицы, которые не надо очищать
			var exceptions = "partners,services,status,packagespeed,networkzones,accesscategories," +
							"categoriesaccessset,connectbrigads,statuscorrelation,usercategories,additionalstatus," +
							"salesettings,internetsettings";
			parts = exceptions.Split(',');
			foreach (var part in parts)
			{
				tables.RemoveAll(i => i == strategy.TableName(part));
			}

			//Чистим таблицы
			foreach (var name in tables)
			{
				var query = "delete from internet." + name;
				DbSession.CreateSQLQuery(query).ExecuteUpdate();
				DbSession.Flush();
			}
			Console.WriteLine("Cleaning " + tables.Count + " tables");
		}


		public void GenerateObjects()
		{
			CleanDb();
			if (!DbSession.Query<InternetSettings>().Any())
				DbSession.Save(new InternetSettings());
			if (!DbSession.Query<SaleSettings>().Any())
				DbSession.Save(SaleSettings.Defaults());

			GenerateRegions();
			GeneratePlans();
			GenerateAdmins();
			GenerateSwitches();
			GenerateClients();
			GenerateContent();
			GeneratePaymentsAndWriteoffs();
			DbSession.Flush();
		}

		private void GenerateSwitches()
		{
			var @switch = new Switch();
			@switch.Name = "Тестовый коммутатор";
			@switch.PortCount = 24;
			DbSession.Save(@switch);
		}

		private void GenerateRegions()
		{
			var vrn = new City();
			vrn.Name = "Борисоглебск";
			DbSession.Save(vrn);
			var blg = new City();
			blg.Name = "Белгород";
			DbSession.Save(blg);
			var region = new Region();
			region.Name = "Борисоглебск";
			region.RegionOfficePhoneNumber = "8-800-2000-600";
			region.City = vrn;
			DbSession.Save(region);
			region = new Region();
			region.Name = "Белгород";
			region.RegionOfficePhoneNumber = "8-800-123-12-23";
			region.City = blg;
			DbSession.Save(region);
		}

		private void GenerateAdmins()
		{
			Permission permission = new Permission { Name = "TestPermission" };
			DbSession.Save(permission);

			Role role = new Role { Name = "Admin" };
			DbSession.Save(role);

			IList<Role> roles = new List<Role>();
			roles.Add(role);
			var emp = DbSession.Query<Employee>().FirstOrDefault(e => e.Login == Environment.UserName);
			if (emp == null)
			{
				emp = new Employee();
				emp.Name = Environment.UserName;
				emp.Login = Environment.UserName;
				emp.Categorie = 3;
			}
			emp.Roles = roles;
			DbSession.Save(emp);
		}

		private void GenerateClients()
		{
			var normalClient = new Client
			{
				PhysicalClient = new PhysicalClient
				{
					Password = DefaultClientPasword,
					PhoneNumber = "4951234567",
					Email = "test@client.ru",
					Name = "Иван",
					Surname = "Кузнецов",
					Patronymic = "нормальный клиент",
					PassportDate = DateTime.Now.AddYears(-20),
					PassportNumber = "123456",
					PassportSeries = "1234",
					PassportResidention = "УФМС россии по гор. Воронежу, по райнону Северный",
					RegistrationAddress = "г. Борисоглебск, ул Ленина, 20",
					Plan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == "Популярный"),
					Balance = 1000,
					Address = DbSession.Query<Address>().FirstOrDefault(),
					LastTimePlanChanged = DateTime.Now.AddMonths(-2)
				},
				Disabled = false,
				RatedPeriodDate = DateTime.Now,
				FreeBlockDays = 28,
				WorkingStartDate = DateTime.Now.AddMonths(-3),
				Lunched = true
			};
			normalClient.Status = DbSession.Get<Status>(5);
			AttachDefaultServices(normalClient);
			AttachEndpoint(normalClient);
			DbSession.Save(normalClient);
			var lease = CreateLease(normalClient.Endpoints.First());
			DbSession.Save(lease);

			//без паспортных данных
			var nopassportClient = CloneClient(normalClient);
			nopassportClient.Patronymic = "без паспортных данных";
			nopassportClient.PhysicalClient.PassportNumber = "";
			DbSession.Save(nopassportClient);

			//Заблокированный
			var disabledClient = CloneClient(normalClient);
			disabledClient.Name = "Алексей";
			disabledClient.Surname = "Дулин";
			disabledClient.Patronymic = "заблокированный клиент";
			disabledClient.Balance = 0;
			disabledClient.SetStatus(StatusType.NoWorked, DbSession);
			DbSession.Save(disabledClient);

			//Неподключенный клиент
			var unpluggedClient = CloneClient(normalClient);
			unpluggedClient.Name = "Николай";
			unpluggedClient.Surname = "Третьяков";
			unpluggedClient.Patronymic = "неподключенный клиент";
			unpluggedClient.WorkingStartDate = DateTime.Now;
			unpluggedClient.Lunched = false;
			unpluggedClient.Status = DbSession.Get<Status>(1);
			foreach (var service in unpluggedClient.ClientServices)
				service.IsActivated = false;
			DbSession.Save(unpluggedClient);

			//У неподключенного клиента уже есть аренда ip
			//Но нет точки подключения
			lease = DbSession.Query<Lease>().First(i => i.Endpoint == unpluggedClient.Endpoints.First());
			lease.Endpoint = null;
			DbSession.Save(lease);
			unpluggedClient.Endpoints.Remove(unpluggedClient.Endpoints.First());
			DbSession.Flush();

			//Клиент с низким балансом
			var lowBalanceClient = CloneClient(normalClient);
			lowBalanceClient.Name = "Владислав";
			lowBalanceClient.Surname = "Савинов";
			lowBalanceClient.Patronymic = "клиент с низким балансом";
			lowBalanceClient.Balance = lowBalanceClient.Plan.Price / 100 * 5;
			DbSession.Save(lowBalanceClient);

			//Клиент с сервисной заявкой
			var servicedClient = CloneClient(normalClient);
			servicedClient.Patronymic = "клиент заблокированный по сервисной заявке";
			servicedClient.SetStatus(DbSession.Get<Status>((int)StatusType.BlockedForRepair));
			var serviceRequest = new ServiceRequest();
			serviceRequest.BlockNetwork = true;
			serviceRequest.Client = servicedClient;
			serviceRequest.CreationDate = DateTime.Now;
			serviceRequest.Description = "Почему-то не работает интернет";
			DbSession.Save(serviceRequest);
			DbSession.Save(servicedClient);

			//Клиент с услугой добровольная блокировка
			var frozenClient = CloneClient(normalClient);
			frozenClient.Patronymic = "клиент с услугой добровольной блокировки";
			frozenClient.SetStatus(DbSession.Get<Status>((int)StatusType.VoluntaryBlocking));
			var blockAccountService = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb).OfType<BlockAccountService>().FirstOrDefault();
			var clientService = new ClientService {
				BeginDate = DateTime.Now,
				EndDate = DateTime.Now.AddDays(35),
				Service = blockAccountService,
				Client = frozenClient,
				ActivatedByUser = true
			};
			DbSession.Save(frozenClient);
			clientService.ActivateFor(frozenClient, DbSession);
			if (frozenClient.IsNeedRecofiguration)
				SceHelper.UpdatePackageId(DbSession, frozenClient);
			DbSession.Save(frozenClient);

			//Обновляем адреса клиентов, чтобы из БД видеть какой клиент какой
			var clients = DbSession.Query<Client>().ToList();
			foreach (var client in clients)
			{
				var query = "UPDATE clients SET WhoRegistered =\"" + client.Patronymic + "\" WHERE id ="+client.Id;
				DbSession.CreateSQLQuery(query).ExecuteUpdate();
			}
		}

		private Lease CreateLease(ClientEndpoint endpoint)
		{
			var lease = new Lease();
			lease.Port = endpoint.Port;
			lease.Ip = endpoint.Ip;
			lease.Switch = endpoint.Switch;
			lease.Endpoint = endpoint;
			return lease;
		}

		private IList<ClientService> AttachDefaultServices(Client client)
		{
			var services = DbSession.Query<Service>().Where(s => s.Name == "IpTv" || s.Name == "Internet").ToList();
			IList<ClientService> csList =
				services.Select(service => new ClientService { Service = service, Client = client, BeginDate = DateTime.Now, IsActivated = true, ActivatedByUser = true })
					.ToList();
			client.ClientServices = csList;
			return csList;
		}

		private ClientService CloneService(ClientService service)
		{
			var obj = new ClientService {
				Service = service.Service,
				Client = service.Client,
				BeginDate =service.BeginDate,
				IsActivated = service.IsActivated,
				ActivatedByUser = service.ActivatedByUser,
			};
			return obj;
		}
		private Client CloneClient(Client client)
		{
			var obj = new Client
			{
				PhysicalClient = new PhysicalClient
				{
					Password = DefaultClientPasword,
					PhoneNumber = client.PhoneNumber,
					Email = client.Email,
					Name = client.Name,
					Surname = client.Surname,
					Patronymic = client.Patronymic,
					Plan = client.Plan,
					Balance = client.Balance,
					Address = client.Address,
					LastTimePlanChanged = client.LastTimePlanChanged,
					PassportDate = client.PhysicalClient.PassportDate,
					PassportNumber = client.PhysicalClient.PassportNumber,
					PassportSeries = client.PhysicalClient.PassportSeries,
					PassportResidention = client.PhysicalClient.PassportResidention,
					RegistrationAddress = client.PhysicalClient.RegistrationAddress,
				},
				Disabled = client.Disabled,
				RatedPeriodDate = client.RatedPeriodDate,
				FreeBlockDays = client.FreeBlockDays,
				WorkingStartDate = client.WorkingStartDate,
				Lunched = client.Lunched,
				Status = client.Status
			};
			foreach (var item in client.ClientServices) {
				var service = CloneService(item);
				service.Client = obj;
				obj.ClientServices.Add(service);
			}
			foreach (var item in client.Endpoints) {
				var endpoint = AttachEndpoint(obj);
				endpoint.Switch = item.Switch;
				endpoint.Client = obj;
			}
			DbSession.Save(obj);
			var lease = CreateLease(obj.Endpoints.First());
			DbSession.Save(lease);
			return obj;
		}

		private ClientEndpoint AttachEndpoint(Client client)
		{
			var parts = DefaultIpString.Split('.');
			parts[2] = (EndpointIpCounter++).ToString();
			IPAddress addr = IPAddress.Parse(string.Join(".",parts));
			var endpoint = new ClientEndpoint {
				PackageId = client.Plan.PackageId,
				Client = client, 
				Ip = addr,
				Port = 22,
				Switch = DbSession.Query<Switch>().First()
			};
			client.Endpoints.Add(endpoint);
			return endpoint;
		}


		private void GeneratePlans()
		{
			//Тарифы
			var plan = new Plan();
			plan.Price = 300;
			plan.Speed = 30;
			plan.Name = "Популярный";
			plan.IsArchived = false;
			plan.Hidden = false;
			plan.IsServicePlan = false;
			DbSession.Save(plan);
			plan = new Plan();
			plan.Price = 500;
			plan.Speed = 50;
			plan.Name = "Оптимальный";
			plan.IsArchived = false;
			plan.Hidden = false;
			plan.IsServicePlan = false;
			DbSession.Save(plan);
			plan = new Plan();
			plan.Price = 900;
			plan.Speed = 100;
			plan.Name = "Максимальный";
			plan.IsArchived = false;
			plan.Hidden = false;
			plan.IsServicePlan = false;
			DbSession.Save(plan);
			DbSession.Flush();

			//Переходы с тарифов
			var plans = DbSession.Query<Plan>().ToList();
			//var popular = plans.First(i => i.Name.Contains("Популярный"));
			//Переход со всех тарифов
			foreach (var plan1 in plans)
			{
				foreach (var plan2 in plans)
				{
					var transfer = new PlanTransfer();
					transfer.PlanFrom = plan1;
					transfer.PlanTo = plan2;
					transfer.Price = 150;
					transfer.IsAvailableToSwitch = true;
					DbSession.Save(transfer);
				}
			}
		}

		private void GeneratePaymentsAndWriteoffs()
		{
			var client = DbSession.Query<Client>().FirstOrDefault();
			for (int i = 0; i < 10; i++)
			{
				var writeof = new WriteOff();
				writeof.Client = client;
				writeof.MoneySum = i;
				writeof.WriteOffSum = i;
				writeof.WriteOffDate = DateTime.Now.AddDays(-i);


				var userWriteOff = new UserWriteOff();
				userWriteOff.Client = client;
				userWriteOff.Date = DateTime.Now.AddDays(-i);
				userWriteOff.IsProcessedByBilling = true;
				userWriteOff.Sum = i;
				userWriteOff.Comment = "Списание за супер быстрый интернет";

				var payment = new Payment();
				payment.Sum = i;
				payment.PaidOn = DateTime.Now.AddDays(-i);
				payment.Client = client;
				payment.Comment = "Платеж";

				DbSession.Save(writeof);
				DbSession.Save(userWriteOff);
				DbSession.Save(payment);
			}
		}

		public void GenerateContent()
		{
			var newsBlock = new NewsBlock(0);
			newsBlock.Title = "Новость";
			newsBlock.Preview = "Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
								" Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			DbSession.Save(newsBlock);

			newsBlock = new NewsBlock(1);
			newsBlock.Title = "Новость2";
			newsBlock.Preview = "Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
								" Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			DbSession.Save(newsBlock);

			for (int i = 0; i < 3; i++)
			{
				var question = new Question(i);
				question.IsPublished = true;
				question.Text = "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?";
				question.Answer = "Да, это возможно. Вам необходимо преобрести роутер, к которому будет подсоединяться кабель, и который будет раздавать сигнал на 2 или более устройств. Поскольку не все модели роутеров могут корректно работать в сети ООО «Инфорум», перед приобретением роутeра";
				DbSession.Save(question);
			}
		}

		public void ImportSwitchesAddresses()
		{
			//var pasrser = new YandexParser(DbSession);
			//string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switch.xlsx");
			//ISheet sheet;
			//XSSFWorkbook hssfwb;
			//using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
			//{
			//	hssfwb = new XSSFWorkbook(file);
			//}

			//var city = new City { Name = "Белгород" };
			//var region = new Region { City = city, Name = "Белгород", RegionOfficePhoneNumber = "8-200-100-200" };
			//DbSession.Save(city);
			//DbSession.Save(region);

			//sheet = hssfwb.GetSheet("Дома");
			//for (int row = 1; row <= sheet.LastRowNum; row++)
			//{
			//	var yad = pasrser.GetYandexAddress(region.Name, sheet.GetRow(row).GetCell(1).ToString(), sheet.GetRow(row).GetCell(2).ToString());
			//	if (yad != null)
			//	{
			//		CreateSwitchAddresses(yad);
			//	}
			//}

			//city = new City { Name = "Борисоглебск" };
			//region = new Region { City = city, Name = "Борисоглебск", RegionOfficePhoneNumber = "8-200-100-201" };
			//DbSession.Save(city);
			//DbSession.Save(region);

			//string textFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switches.txt");
			//var sb = new StringBuilder();
			//using (var sr = new StreamReader(textFile, Encoding.GetEncoding("windows-1251")))
			//{
			//	String line;
			//	while ((line = sr.ReadLine()) != null)
			//	{
			//		sb.AppendLine(line);
			//	}
			//}
			//string borisoglebsk = sb.ToString();
			//borisoglebsk = RemoveSpaces(borisoglebsk);

			//var streetRows = borisoglebsk.Split(new[] { ';' }).ToList();
			//foreach (var row in streetRows)
			//{
			//	var str = row.Split(':');
			//	if (str.Length > 1)
			//	{
			//		var houseString = str[1];

			//		foreach (var houseNumber in houseString.Split(','))
			//		{
			//			var yandexAddress = pasrser.GetYandexAddress(region.Name, str[0], houseNumber);
			//			if (yandexAddress != null)
			//			{
			//				CreateSwitchAddresses(yandexAddress);
			//			}
			//		}
			//	}
			//}
			//DbSession.Flush();
		}

		private void CreateSwitchAddresses(YandexAddress yandexAddress)
		{
			SwitchAddress switchAddress = null;
			if (yandexAddress.House != null)
			{
				switchAddress = new SwitchAddress();
				switchAddress.House = yandexAddress.House;
			}
			else
			{
				switchAddress = new SwitchAddress();
				switchAddress.Street = yandexAddress.Street;
			}
			switchAddress.IsCorrectAddress = yandexAddress.IsCorrect;
			DbSession.Save(switchAddress);
		}

		public string RemoveSpaces(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !Char.IsWhiteSpace(c))
				.ToArray());
		}

		public void LoginForClient(Client Client)
		{
			Open("Account/Login");
			Assert.That(browser.PageSource, Is.StringContaining("Вход в личный кабинет"));
			var name = browser.FindElementByCssSelector(".Account.Login input[name=username]");
			var password = browser.FindElementByCssSelector(".Account.Login input[name=password]");
			name.SendKeys(Client.Id.ToString());
			password.SendKeys("password");
			browser.FindElementByCssSelector(".Account.Login input[type=submit]").Click();
		}

		public void NetworkLoginForClient(Client Client)
		{
			var endpoint = Client.Endpoints.First();
			var lease = DbSession.Query<Lease>().First(i => i.Endpoint == endpoint);
			var ipstring = lease.Ip.ToString();
			Open("Home?ip="+ipstring);
			Assert.That(browser.PageSource, Is.StringContaining("Протестировать скорость"));
			Open("Personal/Profile");
			Assert.IsTrue(browser.PageSource.Contains("Бонусные программы"));
		}
		public MainBilling GetBilling()
		{
			MainBilling.InitActiveRecord();
			var billing = new MainBilling();
			return billing;
		}
	}
}