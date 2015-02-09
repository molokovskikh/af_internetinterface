using System;
using System.Linq;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Test.Helpers
{
	public class ClientHelper
	{
		public static Client CreateLaywerPerson(ISession session)
		{
			var person = new LawyerPerson {
				Name = "TestClient",
				Balance = 1000,
				Region = session.Query<RegionHouse>().First(r => r.Name == "Воронеж")
			};
			return new Client(person, session.Query<Partner>().First()) {
				BeginWork = DateTime.Now,
				Status = session.Load<Status>((uint)StatusType.Worked)
			};
		}

		public static PhysicalClient PhysicalClient(ISession session)
		{
			var tariff = session.Query<Tariff>().First();
			var internet = session.Query<Internet>().First();
			var iptv = session.Query<IpTv>().First();
			var status = session.Load<Status>((uint)StatusType.Worked);

			var client = new PhysicalClient {
				Apartment = "1",
				Balance = 100,
				CaseHouse = "A",
				City = "VRN",
				Entrance = 1,
				Floor = 1,
				HomePhoneNumber = "111-1222222",
				House = 1,
				Name = "testName",
				PassportDate = DateTime.Today,
				PassportNumber = "123456",
				PassportSeries = "1234",
				Password = CryptoPass.GetHashString(CryptoPass.GeneratePassword()),
				Patronymic = "testOtch",
				PhoneNumber = "111-2223344",
				RegistrationAdress = "vrnReg",
				Street = "testStreet",
				Surname = "testSurn",
				Tariff = tariff,
				WhoGivePassport = "guvd",
				ConnectSum = 555
			};

			client.HouseObj = session.Query<House>()
				.FirstOrDefault(h => h.Street == client.Street && h.Number == client.House && h.Case == client.CaseHouse);
			if (client.HouseObj == null) {
				client.HouseObj = new House(client.Street, client.House.GetValueOrDefault(), session.Query<RegionHouse>().First(i => i.Name.Contains("Воронеж")));
				session.Save(client.HouseObj);
			}

			var internalClient = new Client(client, new Settings(session)) {
				Name = String.Format("{0} {1} {2}", client.Surname, client.Name, client.Patronymic),
				Status = status,
				RatedPeriodDate = DateTime.Now,
				StartNoBlock = DateTime.Now.AddMonths(-1)
			};
			internalClient.ClientServices.Add(new ClientService(internalClient, internet));
			internalClient.ClientServices.Add(new ClientService(internalClient, iptv));
			return client;
		}

		public static Client Client(ISession session)
		{
			return PhysicalClient(session).Client;
		}
	}
}