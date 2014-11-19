using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CassiniDev;
using Inforoom2.Helpers;
using Inforoom2.Models;
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
		//	ImportSwitchesAddresses();
			GeneratePlansAndPrices();

			Permission permission = new Permission { Name = "TestPermission" };
			session.Save(permission);

			Role role = new Role { Name = "Admin" };
			session.Save(role);

			var pass = PasswordHasher.Hash("password");
			var client = new Client {
				Username = "client",
				Password = pass.Hash,
				Salt = pass.Salt,
				PhoneNumber = "4951234567",
				Name = "Иван",
				Surname = "Дулин",
				Patronymic = "Михалыч",
				Plan = session.Get<Plan>(1),
				Balance = 10000
			};

			client.Address = new Address { House = session.Query<House>().FirstOrDefault() };
			session.Save(client);

			IList<Role> roles = new List<Role>();
			roles.Add(role);

			var emp = new Employee() {
				Username = "admin",
				Password = pass.Hash,
				Salt = pass.Salt,
				Roles = roles,
			};
			session.Save(emp);
			session.Flush();
		}

		private static void GeneratePlansAndPrices()
		{
		
			Plan plan1 = new Plan();
			plan1.Name = "Plan1";
			plan1.Price = 5;
			plan1.Speed = 5;
			plan1.PlanTransfers = new List<PlanTransfer>();

			Plan plan2 = new Plan();
			plan2.Name = "Plan2";
			plan2.Price = 5;
			plan2.Speed = 5;
			
			plan1.AddPlanTransfer(plan2, 100);

			session.Save(plan1);
			session.Save(plan2);
			
			session.Flush();

			var dbPlan = session.Get<Plan>(1);
			foreach (var plan in dbPlan.PlanTransfers) {
				var x = plan.IsAvailableToSwitch;
			}
		}

		public static void ImportSwitchesAddresses()
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"switch.xlsx");
			var excelFile = new ExcelQueryFactory(path);
			var query =
				excelFile.Worksheet("Дома").Select(row => new {
					row, item = new {
						Disctrict = row["Район"].Cast<string>(),
						Street = row["Улица"].Cast<string>().Trim(),
						House = row["дом, №"].Cast<string>()
					}
				}).ToList();

			var excelQuery = query.Where(q => q.item.House != null);
			var city = new City { Name = "Белгород" };
			var region = new Region { City = city, Name = "Белгород" };
			session.Save(region);

			var streetsQuery = excelQuery.GroupBy(q => q.item.Street.Trim())
				.Select(r => r.First())
				.ToList();

			IList<Street> streets = streetsQuery.Select(row => new Street() { Name = row.item.Street, District = row.item.Disctrict, Region = region }).ToList();

			foreach (var street in streets) {
				session.Save(street);
			}


			IList<SwitchAddress> addresses = new List<SwitchAddress>();
			var excelItems = excelQuery.Select(r => r.item).ToList();
			foreach (var item in excelItems) {
				string number = string.Empty;
				string housing = string.Empty;
				if (item.House != null) {
					AddressHelper.SplitHouseAndHousing(item.House, ref number, ref housing);
					var house = new House();
					house.Number = number;
					house.Housing = housing;
					house.Street = streets.FirstOrDefault(s => s.Name == item.Street);
					SwitchAddress address = new SwitchAddress();

					address.House = house;
					address.Switch = new Switch {
						Name = string.Format("{0}_{1}_{2}", house.Street.Name, house.Number, house.Housing)
					};
					addresses.Add(address);
				}
			}


			/*	foreach (var q in excelQuery) {
				string number = string.Empty;
				string housing = string.Empty;
				SplitHouseAndHousing(q.item.House, ref number, ref housing);
				SwitchAddress address = new SwitchAddress();
				address.Street = streets.FirstOrDefault(s => s.Name == q.item.Street);
				address.House = housesList.FirstOrDefault(s => s.Number == number && s.Housing == housing);
				addresses.Add(address);
			}*/

			foreach (var switchAddress in addresses) {
				session.Save(switchAddress);
			}


			city = new City { Name = "Борисоглебск" };
			region = new Region { City = city, Name = "Борисоглебск" };

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
				var street = new Street { Name = str[0].Replace("ул. ", "").Replace("мкр. ", "").Replace("пер. ", "").Replace("пр. ", ""), Region = region };

				if (str.Length > 1) {
					var houseString = str[1];
					foreach (var houseNumber in houseString.Split(',')) {
						string number = string.Empty;
						string housing = string.Empty;
						AddressHelper.SplitHouseAndHousing(houseNumber, ref number, ref housing);
						var house = new House();
						house.Number = number;
						house.Housing = housing;
						house.Street = street;
						var switchAddress = new SwitchAddress();
						switchAddress.House = house;
						switchAddress.Switch = new Switch {
							Name = string.Format("{0}_{1}_{2}", house.Street.Name, house.Number, house.Housing)
						};

						session.Save(switchAddress);
					}
				}
			}

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