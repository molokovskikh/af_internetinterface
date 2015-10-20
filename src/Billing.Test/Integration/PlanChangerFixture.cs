using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class PlanChangerFixture : MainBillingFixture
	{
		/// <summary>
		/// Предварительное добавление необходиммых данных, задание нужных значений.
		/// </summary>
		/// <param name="timeout">кол-во дней после подключения тарифа,
		///  через которое тариф будет считаться просроченным;</param>
		/// <param name="targetTariff">присвоение пользователю целевого(true)/быстрого(false) тарифа;</param>
		public void InternetPlanChangerFixtureSettings(int timeout, bool targetTariff = true)
		{
			InitSession();
			session.CreateSQLQuery("delete from Internet.inforoom2_PlanChangerData").ExecuteUpdate();
			// создание необходимых тарифов
			var tariffTarget = new Tariff() {
				Price = 300,
				FinalPriceInterval = 1,
				FinalPrice = 400,
				Name = "Tariff_target",
				Description = "Tariff_target"
			};
			session.Save(tariffTarget);
			var tariffCheap = new Tariff() {
				Price = 300,
				FinalPriceInterval = 1,
				FinalPrice = 400,
				Name = "Tariff_cheap",
				Description = "Tariff_cheap"
			};
			session.Save(tariffCheap);
			var tariffFast = new Tariff() {
				Price = 300,
				FinalPriceInterval = 1,
				FinalPrice = 400,
				Name = "Tariff_fast",
				Description = "Tariff_fast"
			};
			session.Save(tariffFast);
			// указание целевого тарифа, тарифов перехода 
			// и даты завершения работы целевого тарифа (просроченной: -1 день от подключения)
			var planChangerData = new InternetInterface.Models.PlanChangerData() {
				TargetPlan = (int)tariffTarget.Id,
				CheapPlan = (int)tariffCheap.Id,
				FastPlan = (int)tariffFast.Id,
				Timeout = timeout
			};
			session.Save(planChangerData);
			// присвоение целевого тарифа клиенту, указание даты подключения
			client.PhysicalClient.Tariff = targetTariff ? tariffTarget : tariffFast;
			client.PhysicalClient.LastTimePlanChanged = SystemTime.Now();
			session.Update(client);
			// создание сервиса PlanChanger для текущего клиента
			var clientService = new ClientService(client, Service.GetByType(typeof(PlanChanger)));
			clientService.IsActivated = true;
			//изменение статуса клиента на Подлюченный
			client.ShowBalanceWarningPage = false;
			client.SetStatus(Status.Get(StatusType.Worked, session));
			client.Activate(clientService);
			session.Flush();
			// проверка текущего статуса клиента (должен быть Подлюченный)
			Assert.AreEqual(client.ShowBalanceWarningPage, false, "ShowBalanceWarningPage клиента ожидался 'false'.");
		}

		// Удаление возможных связей с тарифами в этом тесте, чтобы не изменять MainBillingFixture 
		public void InternetPlanChangerFixtureAfter()
		{
			session.CreateSQLQuery("delete from Internet.inforoom2_plantransfer").ExecuteUpdate();
			session.CreateSQLQuery("delete from Internet.inforoom2_plantvchannelgroups").ExecuteUpdate();
		}

		[Test(Description = "Проверка блокировки клиентов сервисом PlanChanger с 'просроченным' тарифом")]
		public void InternetPlanChangerFixtureTimeoutTrue()
		{
			InternetPlanChangerFixtureSettings(-1);

			// получение всех сервисов Internet
			var clientsWithTargetClientService = session.Query<ClientService>()
				.Where(s => s.Service is PlanChanger).ToList();
			// проверка срока действия целевого тарифа у клиентов с услугой Internet
			// блокировка клиентов с просроченным тарифом.
			foreach (var targetService in clientsWithTargetClientService) targetService.Service.OnTimer(session, targetService);
			// у текущего клиента действие просрочено, он должен быть заблокирован
			Assert.AreEqual(client.ShowBalanceWarningPage, true, "ShowBalanceWarningPage клиента ожидался 'true'.");

			InternetPlanChangerFixtureAfter();
		}

		[Test(Description = "Проверка блокировки клиентов сервисом PlanChanger с 'действующим' тарифом")]
		public void InternetPlanChangerFixtureTimeoutFalse()
		{
			InternetPlanChangerFixtureSettings(10);

			// получение всех сервисов PlanChanger
			var clientsWithTargetClientService = session.Query<ClientService>()
				.Where(s => s.Service is PlanChanger).ToList();
			// проверка срока действия целевого тарифа у клиентов с услугой PlanChanger
			// блокировка клиентов с просроченным тарифом.
			foreach (var targetService in clientsWithTargetClientService) targetService.Service.OnTimer(session, targetService);
			// у текущего клиента действие не просрочено, он не должен быть заблокирован
			Assert.AreEqual(client.ShowBalanceWarningPage, false, "ShowBalanceWarningPage клиента ожидался 'false'.");

			InternetPlanChangerFixtureAfter();
		}

		[Test(Description = "Проверка отсуствия блокировки клиентов сервисом PlanChanger с другим тарифом")]
		public void InternetPlanChangerFixtureAnotheTariff()
		{
			InternetPlanChangerFixtureSettings(-1, false);

			// получение всех сервисов PlanChanger
			var clientsWithTargetClientService = session.Query<ClientService>()
				.Where(s => s.Service is PlanChanger).ToList();
			// проверка срока действия целевого тарифа у клиентов с услугой PlanChanger
			// блокировка клиентов с просроченным тарифом.
			foreach (var targetService in clientsWithTargetClientService) targetService.Service.OnTimer(session, targetService);
			// у текущего клиента действие просрочено, он должен быть заблокирован
			Assert.AreEqual(client.ShowBalanceWarningPage, false, "ShowBalanceWarningPage клиента ожидался 'false'.");

			InternetPlanChangerFixtureAfter();
		}

		[Test(Description = "Проверка отработки OnTimer у услуги PlanChanger для 'просроченного' тарифа, *проверка на сообщение о разблокировке")]
		public void InternetPlanChangerFixtureBillingExecutionTimeoutTrue()
		{
			const int heightSumm = 10000;
			InternetPlanChangerFixtureSettings(-1);
			// проверка срока действия целевого тарифа у клиентов с услугой PlanChanger
			// блокировка биллингом клиента с просроченным тарифом.
			billing.ProcessPayments();

			// блокировка биллингом клиента отключенного, с положительным балансом
			client.AutoUnblocked = true;
			client.Disabled = true;
			client.PhysicalClient.Balance = heightSumm;
			session.Flush();
			billing.ProcessPayments();
			session.Refresh(client);
			// у текущего клиента действие просрочено, он должен быть заблокирован PlanChanger(ом)
			Assert.AreEqual(client.ShowBalanceWarningPage, true, "ShowBalanceWarningPage клиента ожидался 'true'.");

			var hasMessage = client.Appeals.Where(s => s.Appeal.IndexOf("Клиент был заблокирован в связи с прекращением действия тарифа") != -1).ToList();
			Assert.AreEqual(hasMessage.Count == 1, true, "Отсутствует сообщение о блокировке клиента!");

			client.AutoUnblocked = true;
			client.Disabled = true;
			session.Save(client);

			//блокировка биллингом клиента с не просроченным тарифом (если дни тарифа не сочтены).
			var planChangerData = session.Query<PlanChangerData>().FirstOrDefault();
			planChangerData.Timeout = 1000;
			session.Save(planChangerData);
			session.Flush();
			billing.ProcessPayments();
			session.Refresh(client);
			// у текущего клиента действие не просрочено, он не должен быть заблокирован и у него должно появиться сообщение о том, что он разблокирован.
			Assert.AreEqual(client.ShowBalanceWarningPage, false, "ShowBalanceWarningPage клиента ожидался 'true'.");

			hasMessage = client.Appeals.Where(s => s.Appeal.IndexOf("Клиент был заблокирован в связи с прекращением действия тарифа") != -1).ToList();
			Assert.AreEqual(hasMessage.Count == 1, true, "Кол-во сообщениео блокировке клиента больше, чем ожидалось!");

			InternetPlanChangerFixtureAfter();
		}

		[Test(Description = "Проверка отработки OnTimer у услуги PlanChanger для 'действующего' тарифа")]
		public void InternetPlanChangerFixtureBillingExecutionTimeoutFalse()
		{
			InternetPlanChangerFixtureSettings(100);

			// проверка срока действия целевого тарифа у клиентов с услугой PlanChanger
			// блокировка биллингом клиентов с просроченным тарифом.
			billing.ProcessWriteoffs();
			// у текущего клиента действие не просрочено, он не должен быть заблокирован
			Assert.AreEqual(client.ShowBalanceWarningPage, false, "ShowBalanceWarningPage клиента ожидался 'false'.");

			InternetPlanChangerFixtureAfter();
		}

		[Test(Description = "Проверка отработки OnTimer у услуги PlanChanger - скидка остается прежней")]
		public void InternetPlanChangerFixtureBillingCheckSale()
		{
			const decimal clientSale = 10m;
			InternetPlanChangerFixtureSettings(-1);
			client.Sale = clientSale;
			session.Save(client);
			session.Flush();
			Assert.AreEqual(client.Sale, clientSale, string.Format("До работы биллинга скидка клиента должна быть = {0}.", clientSale));
			billing.ProcessPayments();
			session.Refresh(client);
			Assert.AreEqual(client.Sale, clientSale, string.Format("После работы биллинга скидка клиента также должна быть = {0}.", clientSale));
			InternetPlanChangerFixtureAfter();
		}
	}
}