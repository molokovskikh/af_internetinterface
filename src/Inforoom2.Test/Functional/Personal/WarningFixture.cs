using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Functional.Personal;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;
using Common.Tools;

namespace Inforoom2.Test.Functional.Personal
{
	/// <summary>
	/// В контроллере прописан код, что на странице варнинг берется текущий клиент, если проект запущен в дебаге.
	/// На реальном сайте клиент достается по IP.
	/// </summary>
	public class WarningFixture : PersonalFixture
	{
		protected void TrySetWarningForClient(Client client)
		{
			Assert.That(client.ShowBalanceWarningPage, Is.False,
				"Для чистоты данного теста, warning должен назначаться биллингом");
			var billing = GetBilling();
			billing.ProcessWriteoffs();
			DbSession.Refresh(client);
			Assert.That(client.ShowBalanceWarningPage, Is.True, "Клиенту не отображается страница Warning");
			OpenWarningPage(client);
		}

		public void CheckWarningPageText(string textToCheck)
		{
			Open("/");
			AssertText("НОВОСТИ");
			Open("Warning");
			AssertText(textToCheck);
		}

		[Test(Description = "Низкий баланс - варнинга нет")]
		public void LowBalancePhysical()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			RunBillingProcessWriteoffs(client);
			AssertText("НОВОСТИ");
		}

		[Test(Description = "Баланс отрицательный - варнинг есть")]
		public void NegativeBalancePhysical()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			var payment = new Payment()
			{
				Client = client,
				Sum = 0,
				PaidOn = SystemTime.Now().AddDays(-2),
				RecievedOn = SystemTime.Now().AddDays(-1)
			};
            client.Payments.Add(payment);
			DbSession.Save(payment);
			client.PhysicalClient.Balance = -5;
			DbSession.Save(client);
			DbSession.Flush();
			RunBillingProcessWriteoffs(client);
			AssertText("Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести");
		}

		[Test(Description = "Баланс отрицательный, есть обещанный платеж актуальный - блокировки нет")]
		public void NegativeBalancePhysicalWithDebtWorkServiceActual()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			var payment = new Payment()
			{
				Client = client,
				Sum = 0,
				PaidOn = SystemTime.Now().AddDays(-2),
				RecievedOn = SystemTime.Now().AddDays(-1)
			};
            client.Payments.Add(payment);
			DbSession.Save(payment);
			client.PhysicalClient.Balance = -10;
			var services = DbSession.Query<Service>().Where(s => s.Name == "Обещанный платеж").ToList();
			var csDebtWorkService =
				services.Select(
					service =>
						new ClientService
						{
							Service = service,
							Client = client,
							BeginDate = DateTime.Now,
							IsActivated = true,
							ActivatedByUser = true
						}).FirstOrDefault();
			client.ClientServices.Add(csDebtWorkService);
			DbSession.Save(client);
			DbSession.Flush();
			RunBillingProcessWriteoffs(client);
			AssertText("НОВОСТИ");
		}


		[Test(Description = "Баланс отрицательный, есть обещанный платеж просроченный - блокировка есть")]
		public void NegativeBalancePhysicalWithDebtWorkServiceOverdue()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			var payment = new Payment()
			{
				Client = client,
				Sum = 0,
				PaidOn = SystemTime.Now().AddDays(-2),
				RecievedOn = SystemTime.Now().AddDays(-1)
			};
            client.Payments.Add(payment);
			DbSession.Save(payment);
			client.PhysicalClient.Balance = -10;
			var services = DbSession.Query<Service>().Where(s => s.Name == "Обещанный платеж").ToList();
			var csDebtWorkService =
				services.Select(service => new ClientService
				{
					Service = service,
					Client = client,
					BeginDate = DateTime.Now.AddDays(-10),
					EndDate = DateTime.Now.AddDays(-3),
					IsActivated = true,
					ActivatedByUser = true
				}).FirstOrDefault();
			client.ClientServices.Add(csDebtWorkService);
			DbSession.Save(client);
			DbSession.Flush();
			RunBillingProcessWriteoffs(client);
			AssertText("Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести");
		}

		[Test(Description = "Нет паспортных данных")]
		public void NoPassport()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("без паспортных данных"));
			client.Lunched = false;
			DbSession.Save(client);
			DbSession.Flush();
			LoginForClient(client);
			Open("/Personal/Profile");
			AssertText("свои паспортные данные:"); 
			var textbox = browser.FindElement(By.CssSelector("#physicalClient_PassportNumber"));
			textbox.SendKeys("7121551");
			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			AssertText("Данные успешно заполнены");
			Open("/Personal/Plans");
			AssertText("У вас не заполнены паспортные данные");
			Css(".warning").Click();
			var date = browser.FindElementByCssSelector("input[name='physicalClient.BirthDate']");
			date.Click();
			var popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			Open("warning");
			AssertText("Для заполнения недостающих паспортных данных необходимо обратиться в офис компании");
			Css(".warning").Click();
			AssertText("НОВОСТИ");
		}

		[Test(Description = "Сервисная заявка, блокирующая работу")]
		public void ServiceRequest()
		{
			var client =
				DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("заблокированный по сервисной заявке"));
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.BlockedForRepair), "Клиент не заблокирован");
			OpenWarningPage(client);

			CheckWarningPageText("проведения работ по сервисной заявке");
			Css(".repairCompleted").Click();
			AssertText("Работа возобновлена");
			DbSession.Refresh(client);
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked), "Клиент все еще заблокирован");
			CheckWarningPageText("НОВОСТИ");
		}

		[Test(Description = "Клиент с услугой добровольная блокировка")]
		public void FrozenClient()
		{
			var blockAccountService = DbSession.Query<Service>().OfType<BlockAccountService>().FirstOrDefault();
		 
			var client = DbSession.Query<Client>()
				.ToList()
				.First(i => i.Patronymic.Contains("с услугой добровольной блокировки"));
			var clientService = client.ClientServices.FirstOrDefault(i => i.Service == blockAccountService);
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.VoluntaryBlocking), "Клиент не заблокирован");
			Assert.That(clientService, Is.Not.Null,
				"У клиента нет услуги добровольной блокировки");
			clientService.BeginDate = SystemTime.Now().AddDays(-4);
			DbSession.Save(clientService);
			DbSession.Flush();
			OpenWarningPage(client);
			CheckWarningPageText("Добровольная блокировка");
			Css(".unfreeze").Click();
			AssertText("Работа возобновлена");
			DbSession.Clear(); //Сервис кинет исключения, потому что связи что-то плохо вычищаются @todo подумать
			client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с услугой добровольной блокировки"));
			blockAccountService = DbSession.Query<Service>().OfType<BlockAccountService>().FirstOrDefault();
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked), "Клиент все еще заблокирован");
			Assert.That(client.ClientServices.FirstOrDefault(i => i.Service == blockAccountService), Is.Null,
				"У клиента все еще есть услуги добровольной блокировки");
			CheckWarningPageText("НОВОСТИ");
		}

		[Test(Description = "Редирект на главную неподключенного клиента")]
		public void BadLeaseClient()
		{
			//У неподключенного клиента нет точки подключения
			Logout();
				var lease = DbSession.Query<Lease>().First(i => i.Endpoint == null);
			var ipstr = lease.Ip.ToString();
			Open("Warning?ip=" + ipstr);
			AssertText("Протестировать скорость");
		}

		[Test(Description = "Редирект на главную, в случае если клиент не авторизован")]
		public void NotAuthorisedClient()
		{
			Logout();
			Open("Warning");
			AssertText("Протестировать скорость");
		}

		[Test(
			Description =
				"Проверка фильтрации варнингом контроллеров Personal, Service, Warning и пропуска других, например About")]
		public void FilterCheckFixture()
		{
			//У неподключенного клиента нет точки подключения
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("без паспортных данных"));
			const string textToCheck = "Внимание";
			LoginForClient(client);
			Open("Warning");
			AssertText(textToCheck);
			Open("Personal/Profile");
			AssertNoText(textToCheck);
			Open("Personal/Notifications");
			AssertText(textToCheck);
			Open("Service/BlockAccount");
			AssertText(textToCheck);
			//На главной варнинга быть не должно
			Open("/");
			AssertNoText(textToCheck);
			//На странице "О компании" варнинга быть не должно
			Open("About");
			AssertNoText(textToCheck);
		}

		[Test(Description = "Проверка работы варнинга у юр. лица без флага ShowBalanceWarningPage")]
		public void LawClientNormalFixture()
		{
			Logout();
			var legalClient = DbSession.Query<Client>().ToList().First(i => i.LegalClient != null);
			legalClient.ShowBalanceWarningPage = false;
			legalClient.Disabled = false;
			DbSession.Save(legalClient);
			Assert.That(legalClient, Is.Not.EqualTo(null), "В БД отсутствует юр.лицо");
			Assert.That(legalClient.Endpoints.Count(s => !s.Disabled) > 0, Is.EqualTo(true), "У юр. лица отсутствует точка подключения");
			Open("Warning?ip=" + legalClient.Endpoints.First(s => !s.Disabled).Ip);
			AssertText("Протестировать скорость");
		}

		[Test(Description = "Проверка работы варнинга у юр. лица с флагом ShowBalanceWarningPage")]
		public void LawClientLowBalanceFixture()
		{
			Logout();
			var legalClient = DbSession.Query<Client>().ToList().First(i => i.LegalClient != null);
			legalClient.ShowBalanceWarningPage = true;
			legalClient.Disabled = false;
			DbSession.Save(legalClient);
			DbSession.Flush();
			Assert.That(legalClient, Is.Not.EqualTo(null), "В БД отсутствует юр.лицо");
			Assert.That(legalClient.Endpoints.Count(s => !s.Disabled) > 0, Is.EqualTo(true), "У юр. лица отсутствует точка подключения");
			Open("Warning?ip=" + legalClient.Endpoints.First(s => !s.Disabled).Ip);
			AssertText("Вам необходимо оплатить услуги, в противном случае доступ в Интернет будет заблокирован");
			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			AssertText("Протестировать скорость");
			DbSession.Refresh(legalClient);
			var hasAppealMessage =
				legalClient.Appeals.Any(s => s.Message.IndexOf("Отключена страница Warning, клиент отключил со страницы") != -1);
			Assert.That(hasAppealMessage, Is.EqualTo(true),
				"На странице клиента (в админке) должно появиться сообщение, о том, что клиент отключил варнинг (он осведомлен).");
			Assert.That(legalClient.ShowBalanceWarningPage, Is.EqualTo(false),
				"Варнинг не должен показываться после того, как пользователь сообщил, что он осведоллен (нажав на кнопку).");
		}

		[Test(Description = "Проверка работы варнинга у юр. лица с флагом ShowBalanceWarningPage")]
		public void LawClientDisabledFixture()
		{
			Logout();
			var legalClient = DbSession.Query<Client>().ToList().First(i => i.LegalClient != null);
			legalClient.ShowBalanceWarningPage = true;
			legalClient.Disabled = true;
			DbSession.Save(legalClient);
			DbSession.Flush();
			Assert.That(legalClient, Is.Not.EqualTo(null), "В БД отсутствует юр.лицо");
			Assert.That(legalClient.Endpoints.Count(s => !s.Disabled) > 0, Is.EqualTo(true), "У юр. лица отсутствует точка подключения");
			Open("Warning?ip=" + legalClient.Endpoints.First(s => !s.Disabled).Ip);
			AssertText("Для продолжения работы в интернете необходимо оплатить услуги.");
			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			AssertText("Протестировать скорость");
		}

		[Test(Description = "Редирект на целевую страницу, если клиент валидный"), Ignore("Временно отключен")]
		public void JustToRedirectClient()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("нормальный клиент"));
			LoginForClient(client);
			Open("/");
			var endpoint = client.Endpoints.First(s=> !s.Disabled);
			var lease = DbSession.Query<Lease>().First(i => i.Endpoint == endpoint);
			var ipstr = lease.Ip.ToString();
			string testUrl = BuildTestUrl("Personal/Profile");
			string queryString = string.Format("Warning?ip={0}&host={1}", ipstr, testUrl);
			Open(queryString);
			AssertText("ЛИЧНЫЙ КАБИНЕТ: ПРОФИЛЬ");
		}
	}
}