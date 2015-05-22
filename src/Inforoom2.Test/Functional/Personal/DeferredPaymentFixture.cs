﻿using System;
using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
using Inforoom2.Test.Functional.Personal;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	internal class DeferredPaymentFixture : PersonalFixture
	{
		[Test(Description = "Проверка корректной активации услуги 'Обещанный платеж'- клиенту предоставляется бесплатный доступ в интернет на 3дня.")]
		public void ActivateDeferredPayment()
		{
			var clientMark = ClientCreateHelper.ClientMark.disabledClient.GetDescription();

			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.NoWorked, client.Status.Type, "Клиент не имеет статус 'Заблокирован'");
			Assert.IsTrue(client.WorkingStartDate.HasValue, "У клиента не выставлена дата подключения");

			client.ShowBalanceWarningPage = true;
			DbSession.Update(client);
			DbSession.Flush();

			LoginForClient(client);
			DbSession.Refresh(client);
			Assert.IsTrue(client.ShowBalanceWarningPage, "Страница 'Warning' не подключена");

			var button = browser.FindElementByLinkText("Услуги");
			button.Click();
			button = browser.FindElementByLinkText("Подключить");
			button.Click();
			button = browser.FindElementByCssSelector("input[value=Подключить]");
			button.Click();

			DbSession.Refresh(client);
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент не имеет статус 'Подключен'");
			Assert.IsFalse(client.ShowBalanceWarningPage, "Страница 'Warning' по-прежнему подключена");
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