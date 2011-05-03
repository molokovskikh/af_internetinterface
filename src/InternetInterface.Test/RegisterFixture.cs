using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.ActiveRecord.Framework.Internal;
using Castle.Components.Validator;
using InternetInterface.Controllers;
using InternetInterface.Models;
using log4net.Config;
using NUnit.Framework;


namespace NHibernateFixtute.RegisterTest
{
	[TestFixture]
	public class RegisterFixture : RegisterController
	{

		[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterfaceFixture"),
				}, ActiveRecordSectionHandler.Instance);
		}

		[Test]
		public void SearchTets()
		{

			var c = new PhysicalClients
			{
				//AdressConnect = "",
				//Balance = 0,
				City = "",
				/*WhoRegistered = new Partner
				{
					Adress = "TestAdr",
					Email = "TestEmail",
					Login = "TestLogin",
					Name = "TestName",
					//Pass = "TestPass",
					TelNum = "TestTel"
				},*/
				//Login = "",
				Name = "dfg",
				PassportNumber = "",
				PassportSeries = "ASDAD",
				Password = "qw",
				RegistrationAdress = "",
				//RegDate = DateTime.Now,
				WhoGivePassport = "",
				Tariff = new Tariff
				         	{
				         		Name = "",
								Description = "",
								Price = 0
				         	},
				Patronymic = "",
				Surname = "",
			};
			Validator = new ValidatorRunner(ActiveRecordModelBuilder.ValidatorRegistry);

			//var b = RegistrLogicClient(c, false, 4);
			var err = c.GetErrorText("Name");
			Console.WriteLine(err);
			//var browser = Open()
		}
	}
}


