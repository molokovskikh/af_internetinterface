using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class SearchFixture : ClientFunctionalFixture
	{
		[SetUp]
		public void Setup()
		{
			Open("Search/SearchUsers.rails");
		}

		[Test]
		public void SortTest()
		{
			browser.Button(Find.ById("SearchButton")).Click();
			browser.Link("head_id").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_name").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_adress").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_regDate").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_tariff").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_balance").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
			browser.Link("head_status").Click();
			Assert.That(browser.Text, Is.StringContaining("Номер счета"));
		}

		[Test]
		public void TariffSortTest()
		{
			var disabledClient = ClientHelper.Client();
			disabledClient.Name = "disabledClient";
			disabledClient.Disabled = true;
			session.Save(disabledClient);

			Open("Search/SearchUsers.rails");
			browser.RadioButton("filter.EnabledTypeProperties.Type_0").Checked = true;
			browser.Button("SearchButton").Click();
			AssertText(Client.Name);
			Assert.That(browser.Text, !Is.StringContaining(disabledClient.Name));
			browser.RadioButton("filter.EnabledTypeProperties.Type_1").Checked = true;
			browser.Button("SearchButton").Click();
			Assert.That(browser.Text, Is.StringContaining(disabledClient.Name));
		}

		[Test]
		public void SearchTest()
		{
			Assert.That(browser.Text, Is.StringContaining("Поиск пользователей"));
			Assert.That(browser.Text, Is.StringContaining("Введите текст для поиска:"));
			Assert.That(browser.Text, Is.StringContaining("Автоматически"));
			Assert.That(browser.Text, Is.StringContaining("Все"));
			Assert.That(browser.Text, Is.StringContaining("Статистика"));
			Assert.That(browser.Text, Is.StringContaining("On-Line клиенты"));
			Assert.That(browser.Text, Is.StringContaining("Уникальных клиентов за сутки"));
			Assert.That(browser.Text, Is.StringContaining("Количество зарегистрированных клиентов"));
			Assert.That(browser.Text, Is.StringContaining("Количество клиентов \"Заблокирован\" и \"Он-Лайн\""));
			Assert.That(browser.Text, Is.StringContaining("Количество клиентов в состоянии \"Заблокирован\""));
			Css("#SearchText").TypeText(Client.Id.ToString());
			browser.Button(Find.ById("SearchButton")).Click();
			Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
		}

		[Test]
		public void ClientInfoTest()
		{
			Open(ClientUrl);
			Assert.That(browser.Text, Is.StringContaining("Город"));
			Assert.That(browser.Text, Is.StringContaining("Паспортные данные"));
			Assert.That(browser.Text, Is.StringContaining("Тариф"));
			Assert.That(browser.Text, Is.StringContaining("Платежи"));
			Assert.That(browser.Text, Is.StringContaining("Регистрационные данные"));
			Assert.That(browser.Text, Is.StringContaining("Паспортные данные"));
			Assert.That(browser.Text, Is.StringContaining("Личная информация"));
			browser.Button(Find.ById("SaveButton")).Click();
			Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
			browser.TextField("BalanceText").AppendText("1000");
			browser.Button(Find.ById("ChangeBalanceButton")).Click();
			Assert.That(browser.Text, Is.StringContaining("Платеж ожидает обработки"));
		}
	}
}