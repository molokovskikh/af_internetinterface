using System;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
using Inforoom2.Test.Functional.Personal;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Personal
{
	internal class DeferredPaymentFixture : PersonalFixture
	{
		public void CheckWarningPageText(string textToCheck)
		{
			Open("Warning");
			AssertText(textToCheck);
		}

		[Test(Description = "Проверка корректной активации услуги 'Обещанный платеж'- клиенту предоставляется бесплатный доступ в интернет на 3дня.")]
		public void ActivateDeferredPayment()
		{
			var clientMark = ClientCreateHelper.ClientMark.disabledClient.GetDescription();

			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			client.Balance = -1;
			var payment = new Payment()
			{
				Client = client,
				Sum = 0,
				PaidOn = SystemTime.Now().AddDays(-2),
				RecievedOn = SystemTime.Now().AddDays(-1)
			};
            client.Payments.Add(payment);
			DbSession.Save(payment);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.NoWorked, client.Status.Type, "Клиент не имеет статус 'Заблокирован'");
			Assert.IsTrue(client.WorkingStartDate.HasValue, "У клиента не выставлена дата подключения");

			DbSession.Update(client);
			DbSession.Flush();

			LoginForClient(client);
			DbSession.Refresh(client);

			CheckWarningPageText("Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести");
			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			button = browser.FindElementByLinkText("Подключить");
			button.Click();
			button = browser.FindElementByCssSelector("input[value=Подключить]");
			button.Click();
			AssertText("Услуга \"Обещанный платеж\" активирована на период");

			DbSession.Refresh(client);
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент не имеет статус 'Подключен'");
			CheckWarningPageText("НОВОСТИ");
		}

		/// <summary>
		/// Проверка отсутствия возможности повторной активации услуги 'Обещанный платеж',если не прошли 30 дней или если у клиента имеются 
		/// задолженности и отсутсвие абонентской платы.Проверка,что подключить услугу повторно можно только после внесения платежа, покрывающего
		/// задолженности и абонентскую плату.
		/// </summary>
		[Test(Description = "Проверка отсутствия возможности повторной активации услуги 'Обещанный платеж'")]
		public void TryReactivateDeferredPayment()
		{
			ActivateDeferredPayment();

			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Patronymic.Contains("заблокированный клиент"));
			Assert.IsNotNull(client, "Заблокированный клиент не найден");
			client.SetStatus(StatusType.NoWorked, DbSession);
			DbSession.Update(client);

			var lastService = client.ClientServices.OrderBy(cs => cs.BeginDate).LastOrDefault(s => s.Service.Name == "Обещанный платеж");
			Assert.IsNotNull(lastService, "У данного клиента не найдена услуга 'Обещанный платеж'");
			Assert.IsTrue(lastService.BeginDate != null && lastService.BeginDate.Value != null, "lastService.BeginDate == null");
			Assert.IsTrue(lastService.EndDate != null && lastService.EndDate.Value != null, "lastService.EndDate == null");
			lastService.BeginDate = lastService.BeginDate.Value.AddDays(-31);
			lastService.EndDate = lastService.EndDate.Value.AddDays(-31);
			lastService.IsActivated = false;
			lastService.IsDeactivated = true;
			DbSession.SaveOrUpdate(lastService);
			DbSession.Flush();
			// Перезагрузить страницу "Услуги" у клиента
			Open("Personal/Service");
			// Не должно быть возм-ти подключить "Обещанный платеж"
			AssertNoText("Подключить");

			//проверка, что у клиента нет возможности подключить "Обещанный платеж",пока не оплачена вся задолженность
			var tariffPrice = client.Plan.Price;
			var payment1 = new Payment {
				Sum = 0.7m * tariffPrice,
				PaidOn = DateTime.Now,
				Client = client,
				Comment = "70% от цены тарифа"
			};
			DbSession.Save(payment1);
			DbSession.Flush();
			DbSession.Refresh(client);

			// Перезагрузить страницу "Услуги" у клиента
			Open("Personal/Service");
			// Пока ещё не должно быть возм-ти подключить "Обещанный платеж"
			AssertNoText("Подключить");

			var payment2 = new Payment {
				Sum = 0.1m * tariffPrice,
				PaidOn = DateTime.Now,
				Client = client,
				Comment = "Еще 10% от цены тарифа"
			};
			DbSession.Save(payment2);
			DbSession.Flush();
			DbSession.Refresh(client);

			// Перезагрузить страницу "Услуги" у клиента
			Open("Personal/Service");
			// Должна появиться возм-ть подключить "Обещанный платеж"
			AssertText("Подключить");
		}
	}
}