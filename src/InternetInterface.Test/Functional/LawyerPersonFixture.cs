using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Web;

namespace InternetInterface.Test.Functional
{
	public class LawyerPersonFixture : WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.CreateLaywerPerson();
			client.LawyerPerson.Balance = -100000;
			session.Save(client);
		}

		[Test]
		public void Activate_disable_block()
		{
			Open(client.Redirect());
			Click("Управление услугами");
			Click("Отключить блокировки");
			Click("Активировать");
			AssertText("Услуга \"Отключить блокировки\" активирована");
			Click("Управление услугами");
			Click("Отключить блокировки");
			Click("Деактивировать");
			AssertText("Услуга \"Отключить блокировки\" деактивирована");
		}
	}
}