using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers;
using InternetInterface.Models;
using log4net.Config;
//using InternetInterface.Validation;
using NUnit.Framework;

using Castle.MonoRail.Framework;


namespace NHibernateFixtute.RegisterTest
{
	[TestFixture]
	public class RegisterFixture : RegisterClientController
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

			var c = new Client
			{
				AdressConnect = "",
				Balance = 0,
				City = "",
				HasRegistered = new Partner
				{
					Adress = "TestAdr",
					Email = "TestEmail",
					Login = "TestLogin",
					Name = "TestName",
					Pass = "TestPass",
					TelNum = "TestTel"
				},
				Login = "",
				Name = "",
				PassportNumber = "",
				PassportSeries = "ASDAD",
				Password = "qw",
				RegistrationAdress = "",
				RegDate = DateTime.Now,
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
			Validator = new ValidatorRunner(new CachedValidationRegistry());

			//IValidator validatorPas = new NumberValidationAttribute.NumberValidator();
			

			//IValidator validator = new ContactTextValidator();

			//ConfigureValidatorMessage(validator);
			Register(c, false, 4);
			//var browser = Open()
		}
	}
}


