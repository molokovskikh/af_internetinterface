using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CassiniDev;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Helpers;
using LinqToExcel;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
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
			if (session.Query<Client>().ToList().Count != 0) {
				return;
			}
			if (!session.Query<Address>().Any()) {
				ImportSwitchesAddresses();
			}

				if (!session.Query<Plan>().Any(p => p.Name == "Популярный")) {
				GeneratePlansAndPrices();
					
			}
		

			if (!session.Query<Client>().Any()) {
				Permission permission = new Permission {Name = "TestPermission"};
				session.Save(permission);

				Role role = new Role {Name = "Admin"};
				session.Save(role);

				var pass = CryptoPass.GetHashString("password");

				IList<Role> roles = new List<Role>();
				roles.Add(role);

				var emp = new Employee() {
					Name = Environment.UserName,
					Roles = roles,
					Login = Environment.UserName
				};
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

				client.Status = session.Get<Status>(5);

				var services =
					session.Query<Service>().Where(s =>s.Name == "IpTv" || s.Name == "Internet").ToList();
				IList<ClientService> csList =
					services.Select(service => new ClientService {Service = service, Client = client, BeginDate = DateTime.Now, IsActivated = true, ActivatedByUser = true})
						.ToList();
				client.ClientServices = csList;

				var availableServices =
					session.Query<Service>().Where(s => s.Name == "Обещанный платеж" || s.Name == "Добровольная блокировка").ToList();
				availableServices.ForEach(s=>s.IsActivableFromWeb = true);
				IPAddress addr;
				IPAddress.TryParse("192.168.0.1", out addr);
				var endpoint = new ClientEndpoint {PackageId = 19, Client = client, Ip = addr};
				client.Endpoints = new List<ClientEndpoint> {endpoint};
				session.Save(client);
			}

			
			GenerateNewsAndQuestions();
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
			plans.ForEach(p=>p.IsArchived = true);
			plans.ForEach(p=>session.SaveOrUpdate(p));
			var reginon = session.Query<Region>().FirstOrDefault();
			var plan1 = new Plan {
				Name = "Популярный",
				Price = 300,
				Speed = 30,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>(){reginon}
			};

			var plan2 = new Plan {
				Name = "Оптимальный",
				Price = 500,
				Speed = 50,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>(){reginon}
			};

			var plan3 = new Plan {
				Name = "Гениальный",
				Price = 800,
				Speed = 80,
				PlanTransfers = new List<PlanTransfer>(),
				IsArchived = false,
				IsServicePlan = false,
				PackageId = 19,
				Regions = new List<Region>(){reginon}	
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
			for (int i = 0; i < 3; i++) {
				var newsBlock = new NewsBlock(i);
				newsBlock.Title = "Заголовок новости";
				newsBlock.Preview = "Превью новости.С 02.06.2014г. офис интернет провайдера «Инфорум» располагается по новому адресу:г." +
				                    " Борисоглебск, ул. Третьяковская д.6,напротив магазина «Удачный» ";
				newsBlock.CreationDate = DateTime.Now;
				newsBlock.IsPublished = true;
				session.Save(newsBlock);

				var question = new Question(i);
				question.IsPublished = true;
				question.Text = "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?";
				question.Answer = "Да, это возможно. Вам необходимо преобрести роутер, к которому будет подсоединяться кабель, и который будет раздавать сигнал на 2 или более устройств. Поскольку не все модели роутеров могут корректно работать в сети ООО «Инфорум», перед приобретением роутeра";
				session.Save(question);
			}
			
		}

		public static void ImportSwitchesAddresses()
		{
		/*	string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switch.xlsx");
			var excelFile = new ExcelQueryFactory(path);
			var query =
				excelFile.Worksheet("Дома").Select(row => new {
					row,
					item = new {
						Disctrict = row["Район"].Cast<string>(),
						Street = row["Улица"].Cast<string>().Trim(),
						House = row["дом, №"].Cast<string>()
					}
				}).ToList();

			var excelQuery = query.Where(q => q.item.House != null);*/
			var city = new City {Name = "Белгород"};
			var region = new Region {City = city, Name = "Белгород"};
			var address = new Address();
			var house =  new House("86");
			address.House = house;
			address.House.Street = new Street("улица мичурина");
			address.House.Street.Region = region;
			var sw = new SwitchAddress();
			sw.House = house;
			session.Save(region);
			session.Save(address);
			session.Save(sw);

			/*var streetsQuery = excelQuery.GroupBy(q => q.item.Street.Trim())
				.Select(r => r.First())
				.ToList();

			IList<Street> streets =
				streetsQuery.Select(row => new Street() {Name = row.item.Street, District = row.item.Disctrict, Region = region})
					.ToList();

			foreach (var street in streets) {
				session.Save(street);
			}


			IList<SwitchAddress> addresses = new List<SwitchAddress>();
			var excelItems = excelQuery.Select(r => r.item).ToList();
			foreach (var item in excelItems) {
				
				if (item.House != null) {
					var house = new House();
					house.Number = item.House;
				house.Street = streets.FirstOrDefault(s => s.Name == item.Street);
					SwitchAddress address = new SwitchAddress();

					address.House = house;
					address.Switch = new Switch {
						Name = string.Format("{0}_{1}", house.Street.Name, house.Number)
					};
					addresses.Add(address);
				}
			}*/


			

	/*		foreach (var switchAddress in addresses) {
				session.Save(switchAddress);
			}*/


			city = new City {Name = "Борисоглебск"};
			region = new Region {City = city, Name = "Борисоглебск"};

			/*string textFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switches.txt");
			var sb = new StringBuilder();
			using (var sr = new StreamReader(textFile, Encoding.GetEncoding("windows-1251"))) {
				String line;
				while ((line = sr.ReadLine()) != null) {
					sb.AppendLine(line);
				}
			}
			string borisoglebsk = sb.ToString();
			borisoglebsk = RemoveSpaces(borisoglebsk);

			var streetRows = borisoglebsk.Split(new[] {';'}).ToList();
			foreach (var row in streetRows) {
				var str = row.Split(':');
				var street = new Street {
					Name = str[0].Replace("ул. ", "").Replace("мкр. ", "").Replace("пер. ", "").Replace("пр. ", ""),
					Region = region
				};

				if (str.Length > 1) {
					var houseString = str[1];
					foreach (var houseNumber in houseString.Split(',')) {
						string number = string.Empty;
						string housing = string.Empty;
						AddressHelper.SplitHouseAndHousing(houseNumber, ref number, ref housing);
						var house = new House();
						house.Number = number;
						house.Street = street;
						var switchAddress = new SwitchAddress();
						switchAddress.House = house;
						switchAddress.Switch = new Switch {
							Name = string.Format("{0}_{1}", house.Street.Name, house.Number)
						};

						session.Save(switchAddress);
					}
				}
			}*/
			session.Save(city);
			session.Save(region);

			session.Flush();
		}


		private static string RemoveSpaces(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !Char.IsWhiteSpace(c))
				.ToArray());
		}
	}
}