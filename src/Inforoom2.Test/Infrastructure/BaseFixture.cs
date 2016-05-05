using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Mvc;
using Billing;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NUnit.Framework;
using OpenQA.Selenium;
using Test.Support.Selenium;
using Address = Inforoom2.Models.Address;
using Client = Inforoom2.Models.Client;
using ClientEndpoint = Inforoom2.Models.ClientEndpoint;
using ClientService = Inforoom2.Models.ClientService;
using Cookie = OpenQA.Selenium.Cookie;
using House = Inforoom2.Models.House;
using InternetSettings = Inforoom2.Models.InternetSettings;
using Lease = Inforoom2.Models.Lease;
using PackageSpeed = Inforoom2.Models.PackageSpeed;
using Payment = Inforoom2.Models.Payment;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using SaleSettings = Inforoom2.Models.SaleSettings;
using SceHelper = Inforoom2.Helpers.SceHelper;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;
using UserWriteOff = Inforoom2.Models.UserWriteOff;
using WriteOff = Inforoom2.Models.WriteOff;

namespace Inforoom2.Test.Infrastructure
{
	[TestFixture]
	public class BaseFixture : MySeleniumFixture
	{
		protected ISession DbSession;
		protected string DefaultClientPassword = "password";
		protected string HashedDefaultClientPasword;
		protected string DefaultIpString = "105.168.0.1";
		protected List<Address> UnusedClientAddresses;
		protected ClientCreateHelper ClientHelper = new ClientCreateHelper();

		protected int EndpointIpCounter;

		private Address GetUnusedAddress()
		{
			if (UnusedClientAddresses.Count == 0) {
				return null;
			}
			var obj = UnusedClientAddresses.First();
			UnusedClientAddresses.RemoveAt(0);
			return obj;
		}

		[SetUp]
		public override void IntegrationSetup()
		{
			//Ставим куки, чтобы не отображался popup
			DbSession = MvcApplication.SessionFactory.OpenSession();
			HashedDefaultClientPasword = Inforoom2.Helpers.CryptoPass.GetHashString(DefaultClientPassword);

			// TODO:UnusedClientAddresses
			UnusedClientAddresses = new List<Address>();
			try {
				SetCookie("userCity", "Белгород");
			}
			catch (Exception e) {
			}
			GenerateObjects();
			var allTabsToClose = GlobalDriver.WindowHandles.ToList(); 
			browser.SwitchTo().Window(allTabsToClose[0]);
		}

		public void CloseAllTabsButOne()
		{
			var allTabsToClose = GlobalDriver.WindowHandles.ToList();

			if (allTabsToClose.Count > 1)
				for (int i = 1; i < allTabsToClose.Count; i++) {
					if (GlobalDriver.CurrentWindowHandle != allTabsToClose[i]) {
						GlobalDriver.SwitchTo().Window(allTabsToClose[i]);
						GlobalDriver.Close();
					}
				}
		}

		[TearDown]
		public override void IntegrationTearDown()
		{
			DbSession.Close();
			CloseAllTabsButOne();
		}

		public static void Call(string url)
		{
			if (!string.IsNullOrEmpty(url))
			{
				var wc = new WebClient();
				wc.DownloadString(new Uri(url));
				System.Threading.Thread.Sleep(1000);
				wc.Dispose();
			}

		}
		protected bool IsTextExists(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			return body.Contains(text);
		}

		/// <summary>
		/// Назначает куки. Если значение будет null, то будет произведено удаление
		/// </summary>
		/// <param name="name">Имя куки</param>
		/// <param name="value">Значение куки</param>
		public void SetCookie(string name, string value)
		{
			var text = "";
			var time = SystemTime.Now().AddMonths(1);
			//Если значение null, то убираем куку, не назначая ей значения и ставля текущую дату
			if (value != null)
				text = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
			else
				time = SystemTime.Now();
			var cookie = new Cookie(name, text, null, time);
			browser.Manage().Cookies.AddCookie(cookie);
		}

		public void ClosePreviousTab()
		{
			var tabs2 = browser.WindowHandles.ToList();
			browser.SwitchTo().Window(tabs2[0]);
			browser.Close();
			browser.SwitchTo().Window(tabs2[1]);
		}

		/// <summary>
		/// Получет куки по имени
		/// </summary>
		/// <param name="cookieName">Имя куки</param>
		/// <returns>Значение куки или пустая строка</returns>
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

			var order = "StaticIps,orderservices,orders,planchangerdata,planhtmlcontent,permissiontouser,roletouser,perm_role,user_role,roles,permissions,lawyerperson,plantvchannelgroups,requests,tvchanneltvchannelgroups,tvchannels,"
			            +
			            "lawyerperson,physicalclients,clientendpoints,switchaddress,leases,NetworkSwitches,network_nodes,address,house,street,connectbrigads,banner,slide,regions";


			var parts = order.Split(',');
			foreach (var part in parts) {
				var tablename = strategy.TableName(part);
				tables.Add(tablename);
			}

			////Собираем остальные таблицы при помощи моделей проекта
			var types = Assembly.GetAssembly(typeof (BaseModel)).GetTypes().ToList();
			foreach (var t in types) {
				var attribute = Attribute.GetCustomAttribute(t, typeof (ClassAttribute)) as ClassAttribute;
				if (attribute != null) {
					var name = strategy.TableName(attribute.Table);
					tables.Add(name.ToLower());
				}
			}

			//Удаляем из списка таблицы, которые не надо очищать
			var exceptions =
				"unresolvedphone,clientendpointinternetlogs,internetsessionslogs,partners,services,status,packagespeed,networkzones,accesscategories" +
				"categoriesaccessset,connectbrigads,statuscorrelation,usercategories,additionalstatus," +
				"salesettings,internetsettings,issues,recipients,ignoredinns";
			parts = exceptions.Split(',');
			foreach (var part in parts)
				tables.RemoveAll(i => i == strategy.TableName(part));


			//Чистим таблицы
			foreach (var name in tables) {
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
			EndpointIpCounter = 0;
			//GenerateServices();
			GenerateTvProtocols();
			GenerateTvChannels();
			GenerateTvChannelGroups();
			GenerateRegions();
			GenerateAddresses();
			GeneratePlans();
			GenerateSlides();
			GenerateAdmins();
			GenerateSwitches();
			GenerateClients();
			GenerateLegalClient();
			GenerateContent();
			GeneratePaymentsAndWriteoffs();
			GenerateAgent();
			GenerateEmployeeSettings();
            DbSession.Flush();
		}

		//private void GenerateServices()
		//{
		//    var deferredPayment = new DeferredPayment();
		//       deferredPayment.Name = "Обещанный платеж";
		//       deferredPayment.BlockingAll = false;
		//       deferredPayment.IsActivableFromWeb = true;
		//       deferredPayment.Price = 0;
		//       deferredPayment.Description = "DebtWork";
		//       DbSession.Save(deferredPayment);

		//       var blockAccountService = new BlockAccountService();
		//       blockAccountService.Name = "Добровольная блокировка";
		//       blockAccountService.BlockingAll = true;
		//       blockAccountService.IsActivableFromWeb = true;
		//       blockAccountService.Price = 0;
		//       blockAccountService.Description = "VoluntaryBlockin";
		//       DbSession.Save(blockAccountService);

		//       var workLawyer = new WorkLawyer();
		//       workLawyer.Name = "Отключить блокировки";
		//       workLawyer.BlockingAll = false;
		//       workLawyer.IsActivableFromWeb = false;
		//       workLawyer.Price = 0;
		//       workLawyer.Description = "WorkLawyer";
		//       DbSession.Save(workLawyer);

		//       var internet = new Internet();
		//       internet.Name = "Internet";
		//       internet.BlockingAll = false;
		//       internet.IsActivableFromWeb = false;
		//       internet.Price = 0;
		//       internet.Description = "Internet";
		//       DbSession.Save(internet);

		//       var ipTv = new IpTv();
		//       ipTv.Name = "IpTv";
		//       ipTv.BlockingAll = false;
		//       ipTv.IsActivableFromWeb = false;
		//       ipTv.Price = 0;
		//       ipTv.Description = "IpTv";
		//       DbSession.Save(ipTv);

		//       var ipTvBoxRent = new IpTvBoxRent();
		//       ipTvBoxRent.Name = "Аренда приставки";
		//       ipTvBoxRent.BlockingAll = false;
		//       ipTvBoxRent.IsActivableFromWeb = false;
		//       ipTvBoxRent.Price = 0;
		//       ipTvBoxRent.Description = "IpTvBoxRent";
		//       DbSession.Save(ipTvBoxRent);

		//       var fixedIp = new FixedIp();
		//       fixedIp.Name = "Фиксированный ip-адрес";
		//       fixedIp.BlockingAll = false;
		//       fixedIp.IsActivableFromWeb = false;
		//       fixedIp.Price = 200;
		//       fixedIp.Description = "PinnedIp";
		//       DbSession.Save(fixedIp);

		//       var hardwareRent = new HardwareRent();
		//       hardwareRent.Name = "Аренда оборудования";
		//       hardwareRent.BlockingAll = false;
		//       hardwareRent.IsActivableFromWeb = false;
		//       hardwareRent.Price = 0;
		//       hardwareRent.Description = "HardwareRent";
		//       DbSession.Save(hardwareRent);

		//       var speedBoost = new SpeedBoost();
		//       speedBoost.Name = "Увеличить скорость";
		//       speedBoost.BlockingAll = false;
		//       speedBoost.IsActivableFromWeb = false;
		//       speedBoost.Price = 0;
		//       speedBoost.Description = "SpeedBoost";
		//       DbSession.Save(speedBoost);

		//       var planChanger = new PlanChanger();
		//       planChanger.Name = "PlanChanger";
		//       planChanger.BlockingAll = false;
		//       planChanger.IsActivableFromWeb = false;
		//       planChanger.Price = 0;
		//       planChanger.Description = "PlanChanger";
		//       DbSession.Save(planChanger);
		//   }

		private void GenerateTvProtocols()
		{
			var names = "udp,rtp,test".Split(',');
			foreach (var name in names) {
				var protocol = new TvProtocol();
				protocol.Name = name;
				DbSession.Save(protocol);
			}
			DbSession.Flush();
		}

		private void GenerateTvChannels()
		{
			var channels = "НТВ,РТР,СТС,МТВ,ТНТ,Культура,Спорт,ППТ".Split(',');
			var ports = "1234,1237,31,189,55,123123,1256,1257".Split(',');
			var urls =
				"224.0.90.160,112.22.11.32,112.32.44.18,112.32.44.18,112.32.44.18,112.32.44.18,112.32.44.18,112.32.44.10".Split(',');
			var protocols = "udp,rtp,udp,rtp,udp,rtp,udp,rtp".Split(',');
			for (var i = 0; i < channels.Count(); i++) {
				var newChannel = new TvChannel();
				newChannel.Name = channels[i];
				newChannel.Port = int.Parse(ports[i]);
				newChannel.Url = urls[i];
				newChannel.Enabled = true;
				newChannel.TvProtocol = DbSession.Query<TvProtocol>().First(j => j.Name == protocols[i]);
				DbSession.Save(newChannel);
			}
		}

		private void GenerateTvChannelGroups()
		{
			var TvChannels = DbSession.Query<TvChannel>().ToList();
			var group = new TvChannelGroup();
			group.Name = "Все";
			foreach (var channel in TvChannels) {
				if (channel.Name != "ППТ")
					group.TvChannels.Add(channel);
			}
			DbSession.Save(group);


			group = new TvChannelGroup();
			group.Name = "Основная";
			group.TvChannels.Add(TvChannels.First(i => i.Name == "СТС"));
			group.TvChannels.Add(TvChannels.First(i => i.Name == "Культура"));
			group.TvChannels.Add(TvChannels.First(i => i.Name == "ТНТ"));
			DbSession.Save(group);

			group = new TvChannelGroup();
			group.Name = "Развлечения";
			group.TvChannels.Add(TvChannels.First(i => i.Name == "СТС"));
			group.TvChannels.Add(TvChannels.First(i => i.Name == "МТВ"));
			group.TvChannels.Add(TvChannels.First(i => i.Name == "ТНТ"));
			DbSession.Save(group);

			group = new TvChannelGroup();
			group.Name = "Спорт";
			group.TvChannels.Add(TvChannels.First(i => i.Name == "РТР"));
			group.TvChannels.Add(TvChannels.First(i => i.Name == "Спорт"));
			DbSession.Save(group);

			group = new TvChannelGroup();
			group.Name = "Детская";
			DbSession.Save(group);
		}

		private void GenerateAgent()
		{
			//активированный агент
			var agent = new Agent();
			agent.Name = "Сарычев Алексей Валерьевич";
			agent.Active = true;
			DbSession.Save(agent);

			agent = new Agent();
			agent.Name = "Павлов Дмитрий";
			agent.Active = true;
			DbSession.Save(agent);

			//дезактивированный агент
			agent = new Agent();
			agent.Name = "Егоров Павел";
			agent.Active = false;
			DbSession.Save(agent);
		}

		private void GenerateEmployeeSettings()
		{
			var  settings = new EmployeeTariff();
			settings.ActionName = "WorkedClient";
			settings.Sum = 101;
			settings.Description = "";
			DbSession.Save(settings);
			settings = new EmployeeTariff();
			settings.ActionName = "AgentPayIndex";
			settings.Sum = 102;
			settings.Description = "";
			DbSession.Save(settings);
		}

		private void GenerateAddresses()
		{
			const string regionName = "Борисоглебск";
			var addressHelper = new AddressCreateHelper();
			do {
				// Создана ли новая запись в таблице House, есть ли смысл добавлять новый адрес
				bool newHousesCreated = false;

				// Проверка на наличие в таб. Region необходимого региона
				var region = DbSession.Query<Region>().FirstOrDefault(s => s.Name == regionName);
				if (region == null) {
					throw new Exception("Заданный регион не найден: " + regionName);
				}
				// Проверка на наличие в таб. Street текущей улицы
				var street = DbSession.Query<Street>().FirstOrDefault(s => s.Name == addressHelper.Street);
				if (street == null) {
					// Если улица в таб. Street отсусствует, создаем новую запись
					street = new Street();
					street.Name = addressHelper.Street;
					street.Geomark = "51.3663252,42.08180200000004";
					street.Region = region;
					DbSession.Save(street);
				}

				// Проверка на наличие в таб. House текущего дома
				var house = DbSession.Query<House>().FirstOrDefault(s => s.Number == addressHelper.House);
				if (house == null) {
					// Если дом в таб. House отсусствует, создаем новую запись
					house = new House();
					house.Number = addressHelper.House;
					house.Geomark = "51.3663252,42.08180200000004";
					house.Street = street;
					DbSession.Save(house);

					newHousesCreated = true;
				}
				// Проверка на наличие в таб. Address текущего дома
				if (newHousesCreated) {
					var address = DbSession.Query<Address>().FirstOrDefault(s => s.House == house);
					if (address == null) {
						// Если дом в таб. Address отсусствует, создаем новую запись
						address = new Address();
						address.Entrance = "1";
						address.Floor = 1;
						address.House = house;
						DbSession.Save(address);
						// Добавляем созданный адрес в таблицу неиспользованных адресов
						UnusedClientAddresses.Add(address);
					}
				}
			} while (addressHelper.GetNextAddress());
		}


		private void GenerateSwitches()
		{
			var @switch = new Switch();
			@switch.Name = "Тестовый коммутатор";
			@switch.PortCount = 24;
			DbSession.Save(@switch);

			var switchAddress = new SwitchAddress();
			switchAddress.House = UnusedClientAddresses[0].House;
			switchAddress.Entrance = 1;
			switchAddress.Street = UnusedClientAddresses[0].House.Street;
			DbSession.Save(switchAddress);

			var networkNode = new NetworkNode();
			networkNode.Name = "Тестовый узел";
			networkNode.Virtual = false;
			networkNode.Addresses.Add(switchAddress);
			networkNode.Switches.Add(@switch);
			DbSession.Save(networkNode);
		}

		private void GenerateRegions()
		{
			var bor = new City();
			bor.Name = "Борисоглебск";
			DbSession.Save(bor);
			var blg = new City();
			blg.Name = "Белгород";
			DbSession.Save(blg);

			var region = new Region();
			region.Name = "Борисоглебск";
			region.RegionOfficePhoneNumber = "8-800-2000-600";
			region.City = bor;
			region.OfficeAddress = "Третьяковская улица д6Б";
			region.OfficeGeomark = "51.3663252,42.08180200000004";
			region.ShownOnMainPage = true;
			DbSession.Save(region);
			var parent = region;

			// Добавление дочернего региона 
			region = new Region();
			region.Name = "Борисоглебск (частный сектор)";
			region.RegionOfficePhoneNumber = "8-800-2000-800";
			region.City = bor;
			region.Parent = parent;
			region.OfficeAddress = "улица Князя Трубецкого д26";
			region.OfficeGeomark = "50.592548,36.59665819999998";
			region.ShownOnMainPage = true;
			DbSession.Save(region);

			region = new Region();
			region.Name = "Белгород";
			region.RegionOfficePhoneNumber = "8-800-123-12-23";
			region.City = blg;
			region.OfficeAddress = "улица Князя Трубецкого д26";
			region.OfficeGeomark = "50.592548,36.59665819999998";
			region.ShownOnMainPage = true;

			DbSession.Save(region);
		}

		private void RenewActionPermissions()
		{
			var testOfInforoomControlPanel =
				GetType().Assembly.GetTypes().FirstOrDefault(i => i.Name == "ControlPanelBaseFixture");
			if (testOfInforoomControlPanel != null) {
				//	new .AdminController().RenewActionPermissions(); 

				var controllers =
					Assembly.Load("InforoomControlPanel")
						.GetTypes()
						.Where(i => i.IsSubclassOf(typeof (InforoomControlPanel.Controllers.ControlPanelController)))
						.ToList();
				foreach (var controller in controllers) {
					var methods = controller.GetMethods();
					var actions = methods.Where(i => i.ReturnType == typeof (ActionResult)).ToList();
					foreach (var action in actions) {
						var name = controller.Name + "_" + action.Name;
						var right = DbSession.Query<Permission>().FirstOrDefault(i => i.Name == name);
						if (right != null)
							continue;
						var newright = new Permission();
						newright.Name = name;
						newright.Description = name;
						DbSession.Save(newright);
					}
				}
			}
		}

		private void GenerateAdmins()
		{
			RenewActionPermissions();
			var permissions = DbSession.Query<Permission>().ToList();

			Role role = new Role {Name = "Admin", Description = "tempRoleDescription", Permissions = permissions};
			DbSession.Save(role);

			IList<Role> roles = new List<Role>();
			roles.Add(role);
			var emp = DbSession.Query<Employee>().FirstOrDefault(e => e.Login == Environment.UserName);
			if (emp == null) {
				emp = new Employee();
				emp.Name = Environment.UserName;
				emp.Login = Environment.UserName;
				emp.Categorie = 3;
				emp.Roles.AddEach(roles);
			}
			emp.Roles = roles;
			DbSession.Save(emp);
			var dealer = new Agent() {Name = emp.Name};
			DbSession.Save(dealer);
		}

		private void GenerateLegalClient()
		{
			var legalClient = new Client
			{
				LegalClient = new LegalClient()
				{
					Balance = 1000,
					Region = DbSession.Query<Region>().First(),
					Name = "Первый юрик на деревне",
					ShortName = "Первый юрик",
					ActualAddress = "ПерваяНаправо, 1",
					LegalAddress = "ПерваяНалево, 4",
					MailingAddress = "ПерваяПрямо, 2",
					ContactPerson = "Семён",
					Inn = "1234567890"
				},
				Status = Status.Get(StatusType.Worked, DbSession),
				SendSmsNotification = false,
				Disabled = false,
				RatedPeriodDate = DateTime.Now,
				FreeBlockDays = 28,
				WorkingStartDate = DateTime.Now.AddMonths(-3),
				Lunched = true,
				Comment = ClientCreateHelper.ClientMark.legalClient.GetDescription(),
				WhoRegistered = DbSession.Query<Employee>().FirstOrDefault(e => e.Login == Environment.UserName)
			};
			// TODO:UnusedClientAddresses
			AttachDefaultServices(legalClient);


			//Создание Endpoint(а)
			var parts = "105.168.3.3".Split('.');
			parts[2] = (EndpointIpCounter++).ToString();
			IPAddress addr = IPAddress.Parse(string.Join(".", parts));

			//SwitchAddress adding based on client address
			var switchAddress = new SwitchAddress();
			switchAddress.House = DbSession.Query<House>().First();
			switchAddress.Entrance = 1;
			switchAddress.Street = DbSession.Query<Street>().First();
			DbSession.Save(switchAddress);

			//CreateSwitch adding based on client address
			var switchItem = CreateSwitch(legalClient);
			var networkNode = new NetworkNode();
			networkNode.Name = string.Format("Узел связи по адресу " + switchAddress.GetFullAddress());
			networkNode.Virtual = false;
			networkNode.Addresses.Add(switchAddress);
			networkNode.Switches.Add(switchItem);
			DbSession.Save(networkNode);
			//CreateSwitch adding based on client address
			var ipPool = DbSession.Query<IpPool>().FirstOrDefault();
			if (ipPool == null) {
				ipPool = new IpPool()
				{
					Begin = 1541080064,
					End = 1541080319,
					LeaseTime = 3600,
					IsGray = false
				};
				DbSession.Save(ipPool);
			}

			//ClientEndpoint adding based on client address
			var endpoint = new ClientEndpoint
			{
				PackageId = 100,
				Client = legalClient,
				Ip = addr,
				Port = 22,
				Switch = switchItem,
				Pool = ipPool
			};
			DbSession.Save(endpoint);
			legalClient.Endpoints.Add(endpoint);
			var lease = CreateLease(endpoint);
			DbSession.Save(lease);
			DbSession.Save(legalClient);
			DbSession.Flush();
		}

		private void GenerateClients()
		{
			var normalClient = new Client
			{
				PhysicalClient = new PhysicalClient
				{
					Password = HashedDefaultClientPasword,
					PhoneNumber = "4951234567",
					Email = "test@client.ru",
					Name = "Иван",
					Surname = "Кузнецов",
					Patronymic = "нормальный клиент",
					PassportDate = DateTime.Now.AddYears(-20),
					BirthDate = DateTime.Now.AddYears(-40),
					PassportNumber = "123456",
					PassportSeries = "1234",
					PassportResidention = "УФМС россии по гор. Воронежу, по райнону Северный",
					RegistrationAddress = "г. Борисоглебск, улица ленина, 20",
					Plan = DbSession.Query<Plan>().First(p => p.Name == "Популярный"),
					Balance = 1000,
					Address = GetUnusedAddress(),
					LastTimePlanChanged = DateTime.Now.AddMonths(-2)
				},
				SendSmsNotification = false,
				Discount = 10,
				Disabled = false,
				RatedPeriodDate = DateTime.Now,
				FreeBlockDays = 28,
				WorkingStartDate = DateTime.Now.AddMonths(-3),
				Lunched = true,
				WhoRegistered = DbSession.Query<Employee>().FirstOrDefault(e => e.Login == Environment.UserName)
			};
			normalClient.Status = DbSession.Get<Status>(5);

			ClientHelper.MarkClient(normalClient, ClientCreateHelper.ClientMark.normalClient);

			// TODO:UnusedClientAddresses
			AttachDefaultServices(normalClient);
			AttachEndpoint(normalClient);
			DbSession.Save(normalClient);

			var lease = CreateLease(normalClient.Endpoints.First());
			DbSession.Save(lease);

			// c тарифом, игнорирующим скидку
			var ignoreDiscountClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.ignoreDiscountClient);
			ignoreDiscountClient.PhysicalClient.Plan = DbSession.Query<Plan>().FirstOrDefault(s => s.IgnoreDiscount);
			DbSession.Save(ignoreDiscountClient);


			//c тарифом, привязанным к региону
			var clientWithPlanOfRegion = CloneClient(normalClient, ClientCreateHelper.ClientMark.planWithRegionClient);
			clientWithPlanOfRegion.PhysicalClient.Plan =
				DbSession.Query<Plan>().First(p => p.Name == "Тариф с указанным регионом");
			clientWithPlanOfRegion.Discount = 10;
			DbSession.Save(clientWithPlanOfRegion);


			//Заблокированный
			var disabledClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.disabledClient);
			disabledClient.Balance = 0;
			disabledClient.SetStatus(StatusType.NoWorked, DbSession);
			DbSession.Save(disabledClient);

			//Неподключенный клиент
			var unpluggedClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.unpluggedClient);
			unpluggedClient.WorkingStartDate = DateTime.Now;
			unpluggedClient.Lunched = false;
			unpluggedClient.Status = DbSession.Get<Status>(1);
			unpluggedClient.PhysicalClient.PassportNumber = "";
			unpluggedClient.PhysicalClient.PassportSeries = "";
			unpluggedClient.PhysicalClient.PassportResidention = "";
			unpluggedClient.PhysicalClient.RegistrationAddress = "";
			unpluggedClient.PhysicalClient.BirthDate = DateTime.MinValue;
			unpluggedClient.PhysicalClient.PassportDate = DateTime.MinValue;
			unpluggedClient.SetStatus(StatusType.BlockedAndConnected, DbSession);
			foreach (var service in unpluggedClient.ClientServices)
				service.IsActivated = false;
			DbSession.Save(unpluggedClient);

			//без паспортных данных
			var nopassportClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.nopassportClient);
			nopassportClient.PhysicalClient.PassportNumber = "";
			nopassportClient.PhysicalClient.PassportSeries = "";
			nopassportClient.PhysicalClient.PassportResidention = "";
			nopassportClient.PhysicalClient.RegistrationAddress = "";
			nopassportClient.PhysicalClient.BirthDate = DateTime.MinValue;
			nopassportClient.PhysicalClient.PassportDate = DateTime.MinValue;
			DbSession.Save(nopassportClient);

			//У неподключенного клиента уже есть аренда ip
			//Но нет точки подключения
			lease = DbSession.Query<Lease>().First(i => i.Endpoint == unpluggedClient.Endpoints.First());
			lease.Endpoint = null;
			DbSession.Save(lease);
			unpluggedClient.Endpoints.Remove(unpluggedClient.Endpoints.First());
			DbSession.Flush();

			//Клиент с низким балансом
			var lowBalanceClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.lowBalanceClient);
			lowBalanceClient.Balance = lowBalanceClient.Plan.Price/100*5;
			DbSession.Save(lowBalanceClient);

			//Клиент с сервисной заявкой
			var servicedClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.servicedClient);
			servicedClient.SetStatus(DbSession.Get<Status>((int) StatusType.BlockedForRepair));
			var serviceRequest = new ServiceRequest();
			serviceRequest.BlockClientAndWriteOffs = true;
			serviceRequest.Client = servicedClient;
			serviceRequest.CreationDate = DateTime.Now;
			serviceRequest.Description = "Почему-то не работает интернет";
			DbSession.Save(serviceRequest);
			DbSession.Save(servicedClient);

			//Клиент с услугой добровольная блокировка
			var frozenClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.frozenClient);
			frozenClient.SetStatus(DbSession.Get<Status>((int) StatusType.VoluntaryBlocking));
			var blockAccountService =
				DbSession.Query<Service>().Where(s => s.IsActivableFromWeb).OfType<BlockAccountService>().FirstOrDefault();
			var clientService = new ClientService
			{
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

			//Клиент с тарифным планом, который закреплен за регионом
			var clientWithRegionalPlan = CloneClient(normalClient, ClientCreateHelper.ClientMark.clientWithRegionalPlan);
			clientWithRegionalPlan.PhysicalClient.Plan = DbSession.Query<Plan>().First(p => p.Name == "50 на 50");
			DbSession.Save(clientWithRegionalPlan);

			//Клиент с домом, регион которого отличается от региона улицы
			var clientWithDifferentRegionHouse = CloneClient(normalClient,
				ClientCreateHelper.ClientMark.clientWithDifferentRegionHouse);
			clientWithDifferentRegionHouse.PhysicalClient.Address.House.Region =
				DbSession.Query<Region>().First(p => p.Name == "Белгород");
			clientWithDifferentRegionHouse.PhysicalClient.Address.House.Street.Region =
				DbSession.Query<Region>().First(p => p.Name == "Борисоглебск");
			DbSession.Save(clientWithDifferentRegionHouse);

			//Новый подключенный клиент,с недавней датой регистрации
			var recentClient = CloneClient(normalClient, ClientCreateHelper.ClientMark.recentClient);
			recentClient.WorkingStartDate = DateTime.Now;
			DbSession.Save(recentClient);

			//Обновляем адреса клиентов, чтобы из БД видеть какой клиент какой
			var clients = DbSession.Query<Client>().ToList();
			foreach (var client in clients) {
				var query = "UPDATE clients SET WhoRegistered =\"" + client.Patronymic + "\" WHERE id =" + client.Id;
				DbSession.CreateSQLQuery(query).ExecuteUpdate();
			}
		}

		private Lease CreateLease(ClientEndpoint endpoint)
		{
			var lease = new Lease();
			lease.Pool = endpoint.Pool;
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
				services.Select(
					service =>
						new ClientService
						{
							Service = service,
							Client = client,
							BeginDate = DateTime.Now,
							IsActivated = true,
							ActivatedByUser = true
						})
					.ToList();
			client.ClientServices = csList;
			return csList;
		}

		private ClientService CloneService(ClientService service)
		{
			var obj = new ClientService
			{
				Service = service.Service,
				Client = service.Client,
				BeginDate = service.BeginDate,
				IsActivated = service.IsActivated,
				ActivatedByUser = service.ActivatedByUser,
			};
			return obj;
		}

		// 
		protected Client CloneClient(Client client, ClientCreateHelper.ClientMark mark)
		{
			var obj = new Client
			{
				PhysicalClient = new PhysicalClient
				{
					Password = HashedDefaultClientPasword,
					PhoneNumber = client.PhoneNumber,
					Email = client.Email,
					/* 	Name = client.Name,
					Surname = client.Surname,
					 */
					Patronymic = client.Patronymic,
					Name = ClientHelper.Name,
					Surname = ClientHelper.Surname,
					//Patronymic = ClientHelper.Patronymic,

					Plan = client.Plan,
					Balance = client.Balance,
					Address = GetUnusedAddress(),
					LastTimePlanChanged = client.LastTimePlanChanged,
					BirthDate = client.PhysicalClient.BirthDate,
					CertificateName = client.PhysicalClient.CertificateName,
					CertificateType = client.PhysicalClient.CertificateType,
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

			ClientHelper.MarkClient(obj, mark);
			ClientHelper.GetNextClient();
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

		/// <summary>
		/// создание свича по адресу клиента,
		/// добавление созданного свича в БД.
		/// </summary>
		/// <param name="client">клиент с адресом</param>
		/// <returns>новый свич</returns>
		private Switch CreateSwitch(Client client)
		{
			// генерация IP
			var parts = DefaultIpString.Split('.');
			parts[2] = (EndpointIpCounter++).ToString();
			IPAddress addr = IPAddress.Parse(string.Join(".", parts));

			var zone = DbSession.Query<Zone>().FirstOrDefault(s => s.Region.Id == client.GetRegion().Id);
			if (zone == null) {
				zone = new Zone();
				zone.Name = client.GetRegion().Name;
				zone.Region = client.GetRegion();
				DbSession.Save(zone);
			}

			// создание свича
			var newSwitch = new Switch();
			newSwitch.Name = "Тестовый коммутатор по адресу " + client.GetAddress();
			newSwitch.Mac = "EC-FE-C5-36-1A-27";
			newSwitch.Ip = addr;
			newSwitch.PortCount = 13;
			DbSession.Save(newSwitch);

			zone.Switches.Add(newSwitch);
			DbSession.Save(zone);

			return newSwitch;
		}

		private ClientEndpoint AttachEndpoint(Client client)
		{
			//IP generating
			var parts = DefaultIpString.Split('.');
			parts[2] = (EndpointIpCounter++).ToString();
			IPAddress addr = IPAddress.Parse(string.Join(".", parts));


			//SwitchAddress adding based on client address
			var switchAddress = new SwitchAddress();
			switchAddress.House = client.PhysicalClient.Address.House;
			switchAddress.Entrance = 1;
			switchAddress.Street = client.PhysicalClient.Address.House.Street;
			DbSession.Save(switchAddress);

			//CreateSwitch adding based on client address
			var switchItem = CreateSwitch(client);
			var networkNode = new NetworkNode();
			networkNode.Name = string.Format("Узел связи по адресу " + switchAddress.GetFullAddress());
			networkNode.Virtual = false;
			networkNode.Addresses.Add(switchAddress);
			networkNode.Switches.Add(switchItem);
			DbSession.Save(networkNode);

			var ipPool = DbSession.Query<IpPool>().FirstOrDefault();
			if (ipPool == null) {
				ipPool = new IpPool()
				{
					Begin = 1541080064,
					End = 1541080319,
					LeaseTime = 3600,
					IsGray = false
				};
				DbSession.Save(ipPool);
			}
			//ClientEndpoint adding based on client address
			var endpoint = new ClientEndpoint
			{
				PackageId = client.Plan.PackageSpeed.PackageId,
				Client = client,
				Ip = addr,
				Port = 22,
				Switch = switchItem,
				Pool = ipPool
			};
			client.Endpoints.Add(endpoint);
			return endpoint;
		}


		private void GeneratePlans()
		{
			var TvChannelGroups = DbSession.Query<TvChannelGroup>().ToList();
			//Тарифы
			var plan = new Plan();
			plan.Price = 300;
			plan.Name = "Популярный";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 35000000);
			plan.TvChannelGroups.Add(TvChannelGroups.First(i => i.Name == "Основная"));
			DbSession.Save(plan);

			plan = new Plan();
			plan.Price = 300;
			plan.IsOnceOnly = true;
			plan.Name = "Одноразовый";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 35000000);
			plan.TvChannelGroups.Add(TvChannelGroups.First(i => i.Name == "Основная"));
			DbSession.Save(plan);

			plan = new Plan();
			plan.Price = 500;
			plan.Name = "Оптимальный";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 30000000);
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			DbSession.Save(plan);

			plan = new Plan();
			plan.Price = 900;
			plan.Name = "Максимальный";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 4000000);
			plan.TvChannelGroups.Add(TvChannelGroups.First(i => i.Name == "Спорт"));
			DbSession.Save(plan);

			plan = new Plan();
			plan.Price = 100m;
			plan.Name = "Тариф-ИгнорДискаунт";
			plan.Disabled = false;
			plan.IgnoreDiscount = true;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 35000000);
			DbSession.Save(plan);


			plan = new Plan();
			plan.Price = 300;
			plan.Name = "Тариф с указанным регионом";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.RegionPlans.Add(new RegionPlan()
			{
				Plan = plan,
				Region = DbSession.Query<Region>().FirstOrDefault()
			});
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 30000000);
			DbSession.Save(plan);

			plan = new Plan();
			plan.Price = 245;
			plan.Name = "Старт";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 4000000);
			DbSession.Save(plan);
			var RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			RegionPlan.Region = DbSession.Query<Region>().First(i => i.Name == "Белгород");
			DbSession.Save(RegionPlan);

			plan = new Plan();
			plan.Price = 245;
			plan.Name = "50 на 50";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Get<PackageSpeed>(17);
			DbSession.Save(plan);
			RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			RegionPlan.Region = DbSession.Query<Region>().First(i => i.Name == "Борисоглебск");
			DbSession.Save(RegionPlan);

			plan = new Plan();
			plan.Price = 300;
			plan.Name = "Народный";
			plan.Disabled = false;
			plan.AvailableForOldClients = false;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = true;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 30000000);
			plan.Comments = "Быстрый, дешевый, есть в тарифах, нет в ЛК";
			DbSession.Save(plan);
			RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			RegionPlan.Region = DbSession.Query<Region>().First(i => i.Name == "Борисоглебск (частный сектор)");
			DbSession.Save(RegionPlan);

			plan = new Plan();
			plan.Price = 600;
			plan.Name = "Народный";
			plan.Disabled = false;
			plan.AvailableForOldClients = true;
			plan.IsServicePlan = false;
			plan.AvailableForNewClients = false;
			plan.PackageSpeed = DbSession.Query<PackageSpeed>().First(i => i.Speed == 30000000);
			plan.Comments = "Быстрый, дорогой, нет в тарифах, есть в ЛК";
			DbSession.Save(plan);
			RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			RegionPlan.Region = DbSession.Query<Region>().First(i => i.Name == "Борисоглебск (частный сектор)");
			DbSession.Save(RegionPlan);

			DbSession.Flush();

			//Переходы с тарифов
			var plans = DbSession.Query<Plan>().ToList();
			//var popular = plans.First(i => i.Name.Contains("Популярный"));
			//Переход со всех тарифов
			foreach (var plan1 in plans) {
				foreach (var plan2 in plans) {
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
			for (int i = 0; i < 10; i++) {
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
			newsBlock.Preview =
				"Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
				" Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			DbSession.Save(newsBlock);

			newsBlock = new NewsBlock(1);
			newsBlock.Title = "Новость2";
			newsBlock.Preview =
				"Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
				" Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			DbSession.Save(newsBlock);

			for (int i = 0; i < 3; i++) {
				var question = new Question(i);
				question.IsPublished = true;
				question.Text = "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?";
				question.Answer =
					"Да, это возможно. Вам необходимо преобрести роутер, к которому будет подсоединяться кабель, и который будет раздавать сигнал на 2 или более устройств. Поскольку не все модели роутеров могут корректно работать в сети ООО «Инфорум», перед приобретением роутeра";
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
			if (yandexAddress.House != null) {
				switchAddress = new SwitchAddress();
				switchAddress.House = yandexAddress.House;
			}
			else {
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

		public void Logout()
		{
			Open("/Account/Logout");
		}

		public Client GetClient(ClientCreateHelper.ClientMark mark)
		{
			return DbSession.Query<Client>().First(i => i.Comment == mark.GetDescription());
		}

		public void NetworkLoginForClient(Client Client)
		{
			var endpoint = Client.Endpoints.First();
			var lease = DbSession.Query<Lease>().First(i => i.Endpoint == endpoint);
			var ipstring = lease.Ip.ToString();
			Open("Home?ip=" + ipstring);
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

		// Добавление слайдов и банеров для каждого региона
		private void GenerateSlides()
		{
			var tariffId = DbSession.Query<Plan>().First(s => s.Name == "Народный" && s.Price == 300);
			var employee = DbSession.Query<Employee>().FirstOrDefault();
			var regionList = DbSession.Query<Region>().ToList();
			for (int i = 0; i < regionList.Count; i++) {
				// слайдер *c изображением на инфорум2
				Slide slide = new Slide();
				slide.Url = "/ClientRequest/RequestFromTariff/" + tariffId.Id;
				slide.ImagePath = "/Images/slider1.png";
				slide.LastEdit = DateTime.Now;
				slide.Enabled = true;
				slide.Partner = employee;
				slide.Priority = i;
				slide.Region = regionList[i];
				DbSession.Save(slide);


				// банер *c изображением на инфорум2 
				Banner banner = new Banner();
				banner.Url = "/ClientRequest/RequestFromTariff/" + tariffId.Id;
				banner.ImagePath = "/Images/actionfolk.png";
				banner.LastEdit = DateTime.Now;
				banner.Enabled = true;
				banner.Partner = employee;
				banner.Region = regionList[i];
				DbSession.Save(banner);
			}
		}

		/// <summary>
		/// Делает паузу теста, что бы карта Яндекса полностью загрузилась и отметила искомый адрес
		/// За меньшее время карта не успевает загрузиться
		/// </summary>
		public void WaitForMap()
		{
			Thread.Sleep(9000);
		}

		protected void RunBillingProcessWriteoffs(Client client = null, bool checkForWarning = true)
		{
			var billing = GetBilling();
			billing.ProcessWriteoffs();
			if (client != null) {
				DbSession.Refresh(client);
				if (checkForWarning) {
					OpenWarningPage(client);
				}
			}
		}

		protected void RunBillingProcessPayments(Client client = null)
		{
			var billing = GetBilling();
			billing.ProcessPayments();
			if (client != null) {
				DbSession.Refresh(client);
			}
		}

		protected void OpenWarningPage(Client client)
		{
			LoginForClient(client);
			var endpoint = client.Endpoints.First();
			var lease = DbSession.Query<Lease>().First(i => i.Endpoint == endpoint);
			var ipstr = lease.Ip.ToString();
			Open("Warning?ip=" + ipstr);
		}
	}
}