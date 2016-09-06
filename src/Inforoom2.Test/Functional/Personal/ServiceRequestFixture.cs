using System;
using System.Collections.Generic;
using System.Linq;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Functional.Personal;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional.Personal
{
	/// <summary>
	/// В контроллере прописан код, что на странице варнинг берется текущий клиент, если проект запущен в дебаге.
	/// На реальном сайте клиент достается по IP.
	/// </summary>
	public class ServiceRequestFixture : PersonalFixture
	{
		protected void RunBilling(Client client)
		{
			var billing = GetBilling();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			DbSession.Refresh(client);
		}

		protected void OpenWarningPage(Client client)
		{
			LoginForClient(client);
			var endpoint = client.Endpoints.First(s => !s.Disabled);
			var lease = DbSession.Query<Lease>().First(i => i.Endpoint == endpoint);
			var ipstr = lease.Ip.ToString();
			Open("Warning?ip=" + ipstr);
		}

		public void CheckWarningPageText(string textToCheck)
		{
			Open("/");
			AssertText("НОВОСТИ");
			//попытка перейти на варнинг
			Open("Warning?ip=" + Client.Endpoints.First().Ip);
			AssertText(textToCheck);
		}

		[Test(Description = "Проверка отключения услуги Internet, при восстановлении работ, создания по одному аппилу на отключение и сохранения состояния disabled = false")]
		public void ServiceRequestBlockForRepair()
		{
			//выставление нормальному клиенту статуса восстановления работ
			var client = Client;
			client.SetStatus(StatusType.BlockedForRepair, DbSession);
			DbSession.DeleteEach(client.Appeals);
			DbSession.Save(client);
			DbSession.Flush();

			//проверка наличия доступа в интернет
			var serviceInternet = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");
			//проверка состояния клиента
			Assert.That(client.Disabled, Is.EqualTo(false), "Клиента должен быть раблокированным, он не блокируется при восстановлении работ");
			Assert.That(serviceInternet.IsActivated, Is.EqualTo(true), "У клиента должен быть доступ в интернет до отработки биллинга");
			//проверка статуса клиента
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.BlockedForRepair), "Клиент не заблокирован");
			//проверка количества сообщений в админке: не доложно быть включения-выключения интернета 
			var appealsCount = client.Appeals.Count(s => s.Client == client && s.Message.IndexOf("услуга \"Internet\"") != -1);
			Assert.That(appealsCount, Is.EqualTo(0), "В админке нет логов об отключении интернета");

			RunBilling(client);
			DbSession.Refresh(client);
			//проверка наличия доступа в интернет
			serviceInternet = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");
			Assert.That(serviceInternet.IsActivated, Is.EqualTo(false), "У клиента доступа в интернет быть не должно");
			//проверка состояния клиента
			Assert.That(client.Disabled, Is.EqualTo(false), "Клиента должен быть раблокированным, он не блокируется при восстановлении работ");
			//проверка количества сообщений в админке 
			appealsCount = client.Appeals.Count(s => s.Message.IndexOf("услуга \"Internet\"") != -1);
			Assert.That(appealsCount, Is.EqualTo(1), "В админке нет логов об отключении интернета");

			//!повторная проверка, чтобы убедиться в том, что сервис не активируется после деактивации
			RunBilling(client);
			DbSession.Refresh(client);
			DbSession.Refresh(serviceInternet);
			//проверка наличия доступа в интернет
			serviceInternet = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");
			Assert.That(serviceInternet.IsActivated, Is.EqualTo(false), "У клиента доступа в интернет быть не должно, сервис деактивируется на время блокировки");
			//проверка статуса клиента
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.BlockedForRepair), "Клиент не заблокирован");
			//проверка количества сообщений в админке 
			appealsCount = client.Appeals.Count(s => s.Message.IndexOf("услуга \"Internet\"") != -1);
			Assert.That(appealsCount, Is.EqualTo(1), "В админке нет логов об отключении интернета");

			//проверка блокирующего сообщения, как следствие, переход к кнопке "возобновления работ"
			CheckWarningPageText("проведения работ по сервисной заявке");
			Css(".repairCompleted").Click();
			AssertText("Работа возобновлена");

			RunBilling(client);
			DbSession.Refresh(client);
			//проверка блокирующего сообщения, всплывать ничего не должно: переводит на главную с варнинга
			CheckWarningPageText("НОВОСТИ");
			//проверка наличия доступа в интернет
			serviceInternet = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");
			Assert.That(serviceInternet.IsActivated, Is.EqualTo(true), "У клиента должен быть доступ в интернет после 'возобновления работы'");
			//проверка статуса клиента
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked), "Клиент все еще заблокирован");
			//проверка количества сообщений в админке 
			appealsCount = client.Appeals.Count(s => s.Message.IndexOf("услуга \"Internet\"") != -1);
			Assert.That(appealsCount, Is.EqualTo(2), "В админке нет логов об отключении интернета");
		}
	}
}