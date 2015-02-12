using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional
{
	/// <summary>
	/// В контроллере прописан код, что на странице варнинг берется текущий клиент, если проект запущен в дебаге.
	/// На реальном сайте клиент достается по IP.
	/// </summary>
	public class WarningFixture : PersonalFixture
	{
		protected void TrySetWarningForClient(Client client)
		{
			Assert.That(client.ShowBalanceWarningPage, Is.False,"Для чистоты данного теста, warning должен назначаться биллингом");
			var billing = GetBilling();
			billing.ProcessWriteoffs();
			DbSession.Refresh(client);
			Assert.That(client.ShowBalanceWarningPage, Is.True, "Клиенту не отображается страница Warning");
			LoginForClient(client);

			Open("Warning");
		}


		[Test(Description = "Низкий баланс")]
		public void LowBalancePhysical()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			TrySetWarningForClient(client);
		
			AssertText("При непоступлении оплаты");
			Css(".warning").Click();
			AssertText("Протестировать скорость");
			DbSession.Refresh(client);
			Assert.That(client.ShowBalanceWarningPage, Is.False, "Клиенту все еще отображается страница Warning");
		}


		[Test(Description = "Нет паспортных данных")]
		public void NoPassport()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("без паспортных данных"));
			TrySetWarningForClient(client);

			AssertText("не заполнены паспортные данные");
			Css(".warning").Click();
			AssertText("паспортные данные");
			DbSession.Refresh(client);
			Assert.That(client.ShowBalanceWarningPage, Is.False, "Клиенту все еще отображается страница Warning");
		}


		[Test(Description = "Сервисная заявка, блокирующая работу")]
		public void ServiceRequest()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("заблокированный по сервисной заявке"));
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.BlockedForRepair), "Клиенту не заблокирован");
			LoginForClient(client);
			Open("Warning");

			AssertText("проведения работ по сервисной заявке");
			Css(".repairCompleted").Click();
			AssertText("Работа возобновлена");
			DbSession.Refresh(client);
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked), "Клиенту все еще заблокирован");
		}

		[Test(Description = "Клиент с услугой добровольная блокировка")]
		public void FrozenClient()
		{
			var blockAccountService = DbSession.Query<Service>().OfType<BlockAccountService>().FirstOrDefault();
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с услугой добровольной блокировки"));

			Assert.That(client.Status.Type, Is.EqualTo(StatusType.VoluntaryBlocking), "Клиент не заблокирован");
			Assert.That(client.ClientServices.FirstOrDefault(i => i.Service == blockAccountService), Is.Not.Null, "У клиента нет услуги добровольной блокировки");

			LoginForClient(client);
			Open("Warning");
			AssertText("Добровольная блокировка");
			Css(".unfreeze").Click();

			AssertText("Работа возобновлена");
			DbSession.Clear(); //Сервис кинет исключения, потому что связи что-то плохо вычищаются @todo подумать
			client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с услугой добровольной блокировки"));
			blockAccountService = DbSession.Query<Service>().OfType<BlockAccountService>().FirstOrDefault();
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked), "Клиенту все еще заблокирован");
			Assert.That(client.ClientServices.FirstOrDefault(i => i.Service == blockAccountService), Is.Null, "У клиента все еще есть услуги добровольной блокировки");
		}

		[Test(Description = "неподключенный клиент"),Ignore]
		public void NewClient()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("неподключенный клиент"));
			Assert.That(client.IsWorkStarted(),Is.False,"Клиент должен быть новым");
			LoginForClient(client);
			Open("Warning");

			AssertText("внесите первый платеж");
			var buttons = browser.FindElementsByCssSelector(".warning");
			Assert.That(buttons.Count,Is.EqualTo(0),"Клиенту не должна отображаться кнопка продолжения работы");
		}
	}
}