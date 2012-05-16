using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using InternetInterface.Models;

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
			return new Client {
				LawyerPerson = person,
				Disabled = false,
				BeginWork = DateTime.Now,
				Status = ActiveRecordBase<Status>.Find((uint)StatusType.Worked)
			};
		}

		public static void CreateClient(Func<PhysicalClients, bool> Ok)
		{
			var client =  new PhysicalClients {
				Apartment = 1,
				Balance = 100,
				CaseHouse = "A",
				City = "VRN",
				Entrance = 1,
				Floor = 1,
				HomePhoneNumber = "1111-22222",
				House = 1,
				Name = "testName",
				PassportDate = DateTime.Now,
				PassportNumber = "123456",
				PassportSeries = "1234",
				Password = CryptoPass.GetHashString(CryptoPass.GeneratePassword()),
				Patronymic = "testOtch",
				PhoneNumber = "8-111-222-33-44",
				RegistrationAdress = "vrnReg",
				Street = "testStreet",
				Surname = "testSurn",
				Tariff = ActiveRecordBase<Tariff>.Find((uint)1),
				WhoGivePassport = "guvd"
			};
			var valid = new ValidatorRunner(new CachedValidationRegistry());
			if (valid.IsValid(client))
			{
				var pay = new Payment {
					Client = Models.Client.Queryable.First(c => c.PhysicalClient == client),
					PaidOn = DateTime.Now,
					RecievedOn = DateTime.Now,
					Sum = 500
				};
				client.SaveAndFlush();
				pay.SaveAndFlush();
				Ok(client);
				client.DeleteAndFlush();
				pay.DeleteAndFlush();
			}
		}

		public static Client Client()
		{
			var physicalClient = new PhysicalClients {
				Name = "Alexandr",
				Surname = "Zolotarev",
				Patronymic = "Alekseevich",
				Street = "Stud",
				House = 12,
				Apartment = 1,
				Entrance = 2,
				Floor = 2,
				PhoneNumber = "8-900-200-80-80",
				Balance = 0,
				Tariff = ActiveRecordLinqBase<Tariff>.Queryable.First(),
				CaseHouse = "sdf",
				City = "bebsk",
				Email = "test@test.ru",
			};
			var client = new Client {
				PhysicalClient = physicalClient,
				BeginWork = null,
				Name =
					String.Format("{0} {1} {2}", physicalClient.Surname, physicalClient.Name,
						physicalClient.Patronymic),
				Status = ActiveRecordBase<Status>.FindFirst()
			};
			return client;
		}
	}
}
