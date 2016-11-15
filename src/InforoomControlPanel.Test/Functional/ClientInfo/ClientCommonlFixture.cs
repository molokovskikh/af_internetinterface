using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.ClientInfo
{
	internal class ClientCommonlFixture : ControlPanelBaseFixture
	{
		private Client CurrentClient;

		/// <summary>
		/// При обычном Flush возникала ошибка, заменил на закрытие сесии - помогло.
		/// </summary>
		private void UpdateDBSession()
		{
			DbSession.Flush();
			DbSession.Close();
			DbSession = DbSession.SessionFactory.OpenSession();
			CurrentClient = DbSession.Query<Client>().First(s => s.Id == CurrentClient.Id);
		}

		[SetUp]
		//в начале 
		public void Setup()
		{
		}

		[Test, Description("Страница клиента. Физ. лицо. Вывод личной информации")]
		public void ListFixture()
		{
			Open("Client/List");
			WaitForText("Список клиентов");
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.PhysicalClient != null);
			browser.FindElementByCssSelector($"[href*='{client.ClientId}']").Click();
			ClosePreviousTab();
			WaitForText("Информация по клиенту");

			Open("Client/List");
			var input = browser.FindElementByCssSelector($"[name='mfilter.filter.Equal.Id']");
			input.Clear();
			input.SendKeys(client.ClientId);
			browser.FindElementByCssSelector($"[value='Поиск']").Click();
			WaitForText("Информация по клиенту");


			Open("Client/List");
			WaitForText("Список клиентов");
			client = DbSession.Query<Client>().FirstOrDefault(s => s.PhysicalClient == null);
			browser.FindElementByCssSelector($"[href*='{client.ClientId}']").Click();
			ClosePreviousTab();
			WaitForText("Информация по клиенту");

			Open("Client/List");
			input = browser.FindElementByCssSelector($"[name='mfilter.filter.Equal.Id']");
			input.Clear();
			input.SendKeys(client.ClientId);
			browser.FindElementByCssSelector($"[value='Поиск']").Click();
			WaitForText("Информация по клиенту");


		}
		
	}
}