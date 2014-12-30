using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CassiniDev;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using Test.Support.Selenium;
using Address = Inforoom2.Models.Address;
using Switch = Inforoom2.Models.Switch;

namespace Inforoom2.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		public static Uri Url;
		public static ISession session = MvcApplication.SessionFactory.OpenSession();


		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			_webServer = SeleniumFixture.StartServer();
			SeedDb();
		}

		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}


		public static void SeedDb()
		{
			var settings = session.Query<InternetSettings>().FirstOrDefault();
			if (settings == null)
				session.Save(new InternetSettings());

			if (!session.Query<SaleSettings>().Any()) {
				session.Save(SaleSettings.Defaults());
			}

			//if (!session.Query<Address>().Any()) {
			//	ImportSwitchesAddresses();
			//	}

			if (!session.Query<Plan>().Any(p => p.Name == "Популярный")) {
				//GeneratePlansAndPrices();
			}

			
			session.Query<City>().ToList().ForEach(i=>session.Delete(i));
			session.Query<Region>().ToList().ForEach(i => session.Delete(i));
			session.Flush();
			if (!session.Query<Region>().Any()) {
				var vrn = new City();
				vrn.Name = "Воронеж";
				session.Save(vrn);
				var blg = new City();
				blg.Name = "Белгород";
				session.Save(blg);
				var region = new Region();
				region.Name = "Воронеж";
				region.RegionOfficePhoneNumber = "8-800-2000-600";
				region.City = vrn;
				session.Save(region);
				region = new Region();
				region.Name = "Белгород";
				region.RegionOfficePhoneNumber = "8-800-123-12-23";
				region.City = blg;
				session.Save(region);
			}


			if (!session.Query<Client>().Any()) {
				Permission permission = new Permission { Name = "TestPermission" };
				session.Save(permission);

				Role role = new Role { Name = "Admin" };
				session.Save(role);

				var pass = CryptoPass.GetHashString("password");

				IList<Role> roles = new List<Role>();
				roles.Add(role);
				var emp = session.Query<Employee>().FirstOrDefault(e => e.Login == Environment.UserName);
				if (emp == null) {
					emp = new Employee();
					emp.Name = Environment.UserName;
					emp.Login = Environment.UserName;
				}
				emp.Roles = roles;
				session.Save(emp);

				var client = new Client {
					PhysicalClient = new PhysicalClient {
						Password = pass,
						PhoneNumber = "4951234567",
						Email = "test@client.rru",
						Name = "Иван",
						Surname = "Дулин",
						Patronymic = "Михалыч",
						Plan = session.Query<Plan>().FirstOrDefault(p => p.Name == "Популярный"),
						Balance = 1000,
						Address = session.Query<Address>().FirstOrDefault(),
						LastTimePlanChanged = DateTime.Now.AddMonths(-2)
					},
					Disabled = false,
					RatedPeriodDate = DateTime.Now,
					FreeBlockDays = 28,
					WorkingStartDate = DateTime.Now
				};

				var client2 = new Client {
					PhysicalClient = new PhysicalClient {
						Password = pass,
						PhoneNumber = "4951234567",
						Email = "test@client.rru",
						Name = "Алексей",
						Surname = "Дулин",
						Patronymic = "Михалыч",
						Plan = session.Query<Plan>().FirstOrDefault(p => p.Name == "Популярный"),
						Balance = 0,
						Address = session.Query<Address>().FirstOrDefault(),
						LastTimePlanChanged = DateTime.Now.AddMonths(-2)
					},
					Disabled = true,
					RatedPeriodDate = DateTime.Now,
					FreeBlockDays = 0,
					WorkingStartDate = DateTime.Now,
					AutoUnblocked = true
				};

				client.Status = session.Get<Status>(5);
				client2.Status = session.Get<Status>(7);

				var services =
					session.Query<Service>().Where(s => s.Name == "IpTv" || s.Name == "Internet").ToList();
				IList<ClientService> csList =
					services.Select(service => new ClientService { Service = service, Client = client, BeginDate = DateTime.Now, IsActivated = true, ActivatedByUser = true })
						.ToList();
				IList<ClientService> csList2 =
					services.Select(service => new ClientService { Service = service, Client = client2, BeginDate = DateTime.Now, IsActivated = true, ActivatedByUser = true })
						.ToList();
				client.ClientServices = csList;
				client2.ClientServices = csList2;

				var availableServices =
					session.Query<Service>().Where(s => s.Name == "Обещанный платеж" || s.Name == "Добровольная блокировка").ToList();
				availableServices.ForEach(s => s.IsActivableFromWeb = true);
				IPAddress addr;
				IPAddress.TryParse("192.168.0.1", out addr);
				var endpoint = new ClientEndpoint { PackageId = 19, Client = client, Ip = addr };
				client.Endpoints = new List<ClientEndpoint> { endpoint };
				session.Save(client);
				session.Save(client2);
			}

			if (!session.Query<Question>().Any()) {
				GenerateNewsAndQuestions();
			}
			GenerateBillInfo();
			session.Flush();
		}

		private static void GenerateBillInfo()
		{
			var client = session.Query<Client>().FirstOrDefault();
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

				session.Save(writeof);
				session.Save(userWriteOff);
				session.Save(payment);
			}
		}

		private static void GeneratePlansAndPrices()
		{
			var plans = session.Query<Plan>().ToList();
			plans.ForEach(p => p.IsArchived = true);
			plans.ForEach(p => session.SaveOrUpdate(p));
			var reginon = session.Query<Region>().FirstOrDefault();
			var plan1 = new Plan {
				Name = "Популярный",
				Price = 300,
				Speed = 30,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>() { reginon }
			};

			var plan2 = new Plan {
				Name = "Оптимальный",
				Price = 500,
				Speed = 50,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>() { reginon }
			};

			var plan3 = new Plan {
				Name = "Гениальный",
				Price = 800,
				Speed = 80,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>() { reginon }
			};

			plan1.AddPlanTransfer(plan2, 100);
			plan1.AddPlanTransfer(plan3, 100);
			plan2.AddPlanTransfer(plan1, 100);
			plan2.AddPlanTransfer(plan3, 100);
			plan3.AddPlanTransfer(plan1, 100);
			plan3.AddPlanTransfer(plan2, 100);

			session.Save(plan1);
			session.Save(plan2);
			session.Save(plan3);

			session.Flush();
		}

		public static void GenerateNewsAndQuestions()
		{
			var client = session.Query<Client>().FirstOrDefault(c => c.PhysicalClient.Name == "Иван");
			if (client == null) {
				return;
			}

			var client2 = session.Query<Client>().FirstOrDefault(c => c.PhysicalClient.Name == "Алексей");
			if (client2 == null) {
				return;
			}
			var newsBlock = new NewsBlock(0);
			newsBlock.Title = client.Id.ToString();
			newsBlock.Preview = "Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
			                    " Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			session.Save(newsBlock);

			newsBlock = new NewsBlock(1);
			newsBlock.Title = client2.Id.ToString();
			newsBlock.Preview = "Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
			                    " Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
			newsBlock.CreationDate = DateTime.Now;
			newsBlock.IsPublished = true;
			session.Save(newsBlock);

			for (int i = 0; i < 3; i++) {
				var question = new Question(i);
				question.IsPublished = true;
				question.Text = "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?";
				question.Answer = "Да, это возможно. Вам необходимо преобрести роутер, к которому будет подсоединяться кабель, и который будет раздавать сигнал на 2 или более устройств. Поскольку не все модели роутеров могут корректно работать в сети ООО «Инфорум», перед приобретением роутeра";
				session.Save(question);
			}
		}

		public static void ImportSwitchesAddresses()
		{
			var pasrser = new YandexParser(session);
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switch.xlsx");
			ISheet sheet;
			XSSFWorkbook hssfwb;
			using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read)) {
				hssfwb = new XSSFWorkbook(file);
			}

			var city = new City { Name = "Белгород" };
			var region = new Region { City = city, Name = "Белгород", RegionOfficePhoneNumber = "8-200-100-200" };
			session.Save(city);
			session.Save(region);

			sheet = hssfwb.GetSheet("Дома");
			for (int row = 1; row <= sheet.LastRowNum; row++) {
				var yad = pasrser.GetYandexAddress(region.Name, sheet.GetRow(row).GetCell(1).ToString(), sheet.GetRow(row).GetCell(2).ToString());
				if (yad != null) {
					CreateSwitchAddresses(yad);
				}
			}

			city = new City { Name = "Борисоглебск" };
			region = new Region { City = city, Name = "Борисоглебск", RegionOfficePhoneNumber = "8-200-100-201" };
			session.Save(city);
			session.Save(region);

			string textFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switches.txt");
			var sb = new StringBuilder();
			using (var sr = new StreamReader(textFile, Encoding.GetEncoding("windows-1251"))) {
				String line;
				while ((line = sr.ReadLine()) != null) {
					sb.AppendLine(line);
				}
			}
			string borisoglebsk = sb.ToString();
			borisoglebsk = RemoveSpaces(borisoglebsk);

			var streetRows = borisoglebsk.Split(new[] { ';' }).ToList();
			foreach (var row in streetRows) {
				var str = row.Split(':');
				if (str.Length > 1) {
					var houseString = str[1];

					foreach (var houseNumber in houseString.Split(',')) {
						var yandexAddress = pasrser.GetYandexAddress(region.Name, str[0], houseNumber);
						if (yandexAddress != null) {
							CreateSwitchAddresses(yandexAddress);
						}
					}
				}
			}
			session.Flush();
		}

		private static void CreateSwitchAddresses(YandexAddress yandexAddress)
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
			session.Save(switchAddress);
		}

		public static string RemoveSpaces(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !Char.IsWhiteSpace(c))
				.ToArray());
		}
	}
}