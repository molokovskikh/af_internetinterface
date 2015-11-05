using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class SearchFixture : ClientFunctionalFixture
	{
		[SetUp]
		public void Setup()
		{
			Open("Search/SearchUsers");
		}

		[Test]
		public void SortTest()
		{
			Css("#SearchButton").Click();
			Css("#head_id").Click();
			AssertText("Номер счета");
			Css("#head_name").Click();
			AssertText("Номер счета");
			Css("#head_adress").Click();
			AssertText("Номер счета");
			Css("#head_regDate").Click();
			AssertText("Номер счета");
			Css("#head_tariff").Click();
			AssertText("Номер счета");
			Css("#head_balance").Click();
			AssertText("Номер счета");
			Css("#head_status").Click();
			AssertText("Номер счета");
		}

		[Test]
		public void TariffSortTest()
		{
			var disabledClient = ClientHelper.Client(session);
			disabledClient.Name = "disabledClient";
			disabledClient.Disabled = true;
			session.Save(disabledClient);

			Open("Search/SearchUsers");
			RunJavaScript("$('#filter_clientTypeFilter_0').click();");
			RunJavaScript("$('#filter_EnabledTypeProperties_0').click();");

			Css("#SearchButton").Click();
			AssertText(Client.Name);
			AssertNoText(disabledClient.Name);
			var radio1 = browser.FindElementById("filter_EnabledTypeProperties_1");
			if (!radio1.Selected)
				radio1.Click();

			Css("#SearchButton").Click();
			WaitForText(disabledClient.Name);
			AssertText(disabledClient.Name);
		}

		[Test]
		public void SearchTest()
		{
			AssertText("Поиск пользователей");
			AssertText("Введите текст для поиска:");
			AssertText("Автоматически");
			AssertText("Все");
			Css("#SearchText").SendKeys(Client.Id.ToString());
			Css("#SearchButton").Click();
			AssertText("Информация по клиенту");
		}

		[Test(Description = "Тестирует подсветку OnLine клиентов на странице поиска клиентов")]
		public void ColourOnlineClientsTest()
		{
			var offlineClient = ClientHelper.Client(session);
			offlineClient.Name = "offline_ColouredClient";
			session.Save(offlineClient);

			var onlineClient = ClientHelper.Client(session);
			onlineClient.Name = "online_ColouredClient";
			session.Save(onlineClient);

			// Занесение в БД объектов, определяющих "onlineClient" как OnLine клиента
			var newNetSwitch = new NetworkSwitch();
			session.Save(newNetSwitch);
			var clientEndpoint = new ClientEndpoint(onlineClient, 1, newNetSwitch);
			session.Save(clientEndpoint);
			var clientLease = new Lease(clientEndpoint);
			clientLease.Ip = new IPAddress(new Random().Next(1000000));
			session.Save(clientLease);

			Open("Search/SearchUsers");
			Css("#SearchText").SendKeys("_ColouredClient");
			Css("#SearchButton").Click();

			// Проверка наличия 1 web-элемента класса ".online_client" на странице браузера
			var cssElemCollection = browser.FindElementsByCssSelector(".online_client");
			Assert.That(cssElemCollection.Count, Is.EqualTo(1));
		}

		[Test, Ignore("'Е' на 'Ё' не заменяется при поиске, данный функционал будет переносится в ближайшем времени (до 12.2015), нет смысла править его в старой админке.")]
		public void SearchYeYoTest()
		{
			var yoClient = ClientHelper.Client(session);
			yoClient.Name = "Том Ёрнинг";
			session.Save(yoClient);

			var yoClient2 = ClientHelper.Client(session);
			yoClient2.Name = "Том Ёрнинг First";
			session.Save(yoClient2);

			var yeClient = ClientHelper.Client(session);
			yeClient.Name = "Том Ернинг";
			session.Save(yeClient);

			var noClient = ClientHelper.Client(session);
			noClient.Name = "Том Рэддл Log";
			session.Save(noClient);
			session.Flush();
			Css("#SearchText").SendKeys("Том");
			Css("#SearchButton").Click();
			AssertText(yoClient.Name);
			AssertText(yeClient.Name);
			AssertText(noClient.Name);
			browser.FindElementByCssSelector("#SearchText").Clear();
			Css("#SearchText").SendKeys("Ёрнинг");
			Css("#SearchButton").Click();

			AssertText(yoClient.Name);
			AssertText(yeClient.Name);
			AssertNoText(noClient.Name);
		}

		[Test]
		public void StatisticTest()
		{
			Open("UserInfo/Statistic");
			AssertText("Статистика");
			AssertText("On-Line клиенты");
			AssertText("Уникальных клиентов за сутки");
			AssertText("Количество зарегистрированных клиентов");
			AssertText("Количество клиентов \"Заблокирован\" и \"Он-Лайн\"");
			AssertText("Количество клиентов \"Заблокирован\"");
		}

		[Test]
		public void ClientInfoTest()
		{
			Open(ClientUrl);
			WaitForText("Тариф");
			//Страница изменения адреса перенесена в новую админку
			//	AssertText("Город");
			AssertText("Паспортные данные");
			AssertText("Тариф");
			AssertText("Платежи");
			AssertText("Регистрационные данные");
			AssertText("Паспортные данные");
			AssertText("Личная информация");
			Css("#SaveButton").Click();
			AssertText("Данные изменены");
			browser.FindElementByName("BalanceText").SendKeys("1000");
			Css("#ChangeBalanceButton").Click();
			AssertText("Платеж ожидает обработки");
		}

		[Test,Ignore("Функционал перенесен в новую админку, + браузер загружает файл в дирректорию по умолчанию*")]
		public void OutOfMemoryExcelExportTest()
		{
			File.Delete("Клиенты.xls");
			var radio1 = browser.FindElementById("filter_clientTypeFilter_1");
			if (!radio1.Selected)
				radio1.Click();
			Click("Выгрузить статистику по клиентам в Excel");
			WaitHelper.WaitOrFail(20.Second(), () => File.Exists("Клиенты.xls"), String.Format("не удалось дождаться файла {0}", Path.GetFullPath("Клиенты.xls")));
			Assert.IsTrue(File.Exists("Клиенты.xls"));
			Click("Поиск");
			AssertText("Поиск пользователей");
		}
	}
}