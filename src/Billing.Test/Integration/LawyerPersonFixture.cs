using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class LawyerPersonFixture : MainBillingFixture
	{
		Client lawyerClient;

		[SetUp]
		public void Up()
		{
			PrepareTest();

			var lPerson = new LawyerPerson {
				Balance = -2000,
				Tariff = 1000m,
			};
			lPerson.Save();
			lawyerClient = new Client {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = lPerson
			};
			lawyerClient.Save();
		}

		[Test]
		public void Three_hours_warning_interval()
		{
			Assert.IsNull(lawyerClient.WhenShowWarning);
			Assert.IsFalse(lawyerClient.SendEmailNotification);
			billing.OnMethod();
			Assert.IsNotNull(lawyerClient.WhenShowWarning);
			Assert.IsTrue(lawyerClient.SendEmailNotification);
			lawyerClient.ShowBalanceWarningPage = false;
			billing.OnMethod();
			Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
			SystemTime.Now = () => DateTime.Now.AddHours(2).AddMinutes(45);
			billing.OnMethod();
			Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
			SystemTime.Now = () => DateTime.Now.AddHours(3);
			billing.OnMethod();
			Assert.IsTrue(lawyerClient.ShowBalanceWarningPage);
			Assert.IsTrue(lawyerClient.SendEmailNotification);
		}
	}
}
