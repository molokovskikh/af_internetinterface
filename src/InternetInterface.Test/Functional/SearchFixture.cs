using System;
using System.IO;
using System.Linq;
using System.Threading;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class SearchFixture : ClientFunctionalFixture
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
			if(!radio1.Selected)
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
		[Test]
		public void SearchYeYoTest()
		{
			var yoClient = ClientHelper.Client(session);
			yoClient.Name = "Том Ёрнинг";
			session.Save(yoClient);

			var yeClient = ClientHelper.Client(session);
			yeClient.Name = "Том Ернинг";
			session.Save(yeClient);

			var noClient = ClientHelper.Client(session);
			noClient.Name = "Том Рэддл";
			session.Save(noClient);
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
			AssertText("Город");
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
		[Test]
		public void OutOfMemoryExcelExportTest()
		{
			File.Delete("Клиенты.xls");
			var radio1 = browser.FindElementById("filter_clientTypeFilter_1");
			if(!radio1.Selected)
				radio1.Click();
			Click("Выгрузить статистику по клиентам в Excel");
			WaitHelper.WaitOrFail(20.Second(), () => File.Exists("Клиенты.xls"), String.Format("не удалось дождаться файла {0}", Path.GetFullPath("Клиенты.xls")));
			Assert.IsTrue(File.Exists("Клиенты.xls"));
			Click("Поиск");
			AssertText("Поиск пользователей");
		}
	}
}
