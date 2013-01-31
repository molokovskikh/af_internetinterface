using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Models.Services;

namespace InternetInterface.Test.Helpers
{
	public class ClientHelper
	{
		public static Client CreateLaywerPerson()
		{
			var person = new LawyerPerson {
				Name = "TestClient",
				Balance = 1000,
				Tariff = 100,
			};
			return new Client() {
				LawyerPerson = person,
				Disabled = false,
				BeginWork = DateTime.Now,
				Status = ActiveRecordBase<Status>.Find((uint)StatusType.Worked)
			};
		}

		public static void CreateClient(Func<Client, bool> Ok)
		{
			var client = PhysicalClient();
			var internalClient = client.Client;
			var valid = new ValidatorRunner(new CachedValidationRegistry());
			if (valid.IsValid(client)) {
				var pay = new Payment {
					Client = internalClient,
					PaidOn = DateTime.Now,
					RecievedOn = DateTime.Now,
					Sum = 500
				};
				internalClient.SaveAndFlush();
				client.SaveAndFlush();
				pay.SaveAndFlush();
				Ok(internalClient);
				client.DeleteAndFlush();
				pay.DeleteAndFlush();
			}
			else {
				throw new Exception(String.Format("Создали невалидного клиента {0}",
					valid.GetErrorSummary(client).ErrorMessages.Implode()));
			}
		}

		public static PhysicalClient PhysicalClient()
		{
			var tariff = ActiveRecordLinqBase<Tariff>.Queryable.First();
			var internet = ActiveRecordLinqBase<Internet>.Queryable.First();
			var iptv = ActiveRecordLinqBase<IpTv>.Queryable.First();
			var status = ActiveRecordBase<Status>.Find((uint)StatusType.Worked);

			var client = new PhysicalClient {
				Apartment = 1,
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

			var internalClient = new Client {
				PhysicalClient = client,
				BeginWork = null,
				Name = String.Format("{0} {1} {2}", client.Surname, client.Name, client.Patronymic),
				Status = status,
				RatedPeriodDate = DateTime.Now,
				StartNoBlock = DateTime.Now.AddMonths(-1)
			};
			internalClient.ClientServices.Add(new ClientService(internalClient, internet));
			internalClient.ClientServices.Add(new ClientService(internalClient, iptv));
			client.Client = internalClient;
			return client;
		}

		public static Client Client()
		{
			return PhysicalClient().Client;
		}
	}
}