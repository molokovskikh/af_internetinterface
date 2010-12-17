using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using InternetInterface;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterfaceFixture.Helpers
{
	class ClientHelperFixture
	{
		//public static PhisicalClients CreateClient(Func<bool> Ok)
		public static void CreateClient(Func<PhisicalClients, bool > Ok)
		{
			var client =  new PhisicalClients
			       	{
			       		Apartment = "1",
			       		Balance = "100",
			       		CaseHouse = "A",
			       		City = "VRN",
			       		Connected = false,
			       		Entrance = "1",
			       		Floor = "1",
			       		HasConnected = null,
			       		HasRegistered = Partner.Find((uint)1),
			       		HomePhoneNumber = "1111-22222",
			       		House = "1",
			       		Login = "Login" + new Random().Next(100),
			       		Name = "testName",
			       		OutputDate = DateTime.Now.ToShortDateString(),
						PassportNumber = "123456",
						PassportSeries = "1234",
						Password = CryptoPass.GetHashString(PhisicalClients.GeneratePassword()),
						Patronymic = "testOtch",
						PhoneNumber = "8-111-222-33-44",
						RegDate = DateTime.Now,
						RegistrationAdress = "vrnReg",
						Street = "testStreet",
						Surname = "testSurn",
						Tariff = Tariff.Find((uint)1),
						WhenceAbout = "ded",
						WhoGivePassport = "guvd"
			       	};
			var valid = new ValidatorRunner(new CachedValidationRegistry());
			if (valid.IsValid(client))
			{
				var pay = new Payment
				          	{
				          		Client = client,
				          		//Agent = InithializeContent.partner,
				          		PaidOn = DateTime.Now,
								RecievedOn = DateTime.Now,
				          		Sum = "500"
				          	};
				client.SaveAndFlush();
				pay.SaveAndFlush();
				Ok(client);
				client.DeleteAndFlush();
				pay.DeleteAndFlush();
				//return client;
			}
			//return null;
		}
	}
}
