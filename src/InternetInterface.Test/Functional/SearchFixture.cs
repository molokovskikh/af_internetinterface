using System;
using System.Linq;
using System.Threading;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class SearchFixture : WatinFixture
	{
		[Test]
		public void FindAdditionalStatus()
		{
			var client = Client.FindFirst();
			var addStat = AdditionalStatus.FindAll();
			client.Status = Status.Find((uint) StatusType.BlockedAndNoConnected);
			client.AdditionalStatus = addStat.First();
			client.Update();

			using (var browser = Open("Search/SearchUsers.rails")) {
				browser.SelectList("addtionalStatus").SelectByValue(addStat.First().Id.ToString());
				browser.Button(Find.ById("SearchButton")).Click();
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining(client.Id.ToString("00000")));
			}
		}

		[Test]
		public void SortTest()
		{
			using (var browser = Open("Search/SearchUsers.rails")) {
				browser.Button(Find.ById("SearchButton")).Click();
				Thread.Sleep(1000);
				browser.Link("head_id").Click();
				Assert.That(browser.Text, Is.StringContaining("Номер счета"));
				browser.Link("head_name").Click();
				Assert.That(browser.Text, Is.StringContaining("Номер счета"));
				browser.Link("head_adress").Click();
				Assert.That(browser.Text, Is.StringContaining("Номер счета"));
				browser.Link("head_telNum").Click();
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
		}

		[Test]
		public void SearchTest()
		{
			using (var browser = Open("Search/SearchUsers.rails")) {
				Assert.That(browser.Text, Is.StringContaining("Поиск пользователей"));
				Assert.That(browser.Text, Is.StringContaining("Введите текст для поиска:"));
				Assert.That(browser.Text, Is.StringContaining("Автоматически"));
				Assert.That(browser.Text, Is.StringContaining("Тариф"));
				Assert.That(browser.Text, Is.StringContaining("Все"));
				Assert.That(browser.Text, Is.StringContaining("Назначено на бригаду"));
				browser.Button(Find.ById("SearchButton")).Click();
				Thread.Sleep(400);
				Assert.That(browser.Text, Is.StringContaining("Номер счета"));
				Assert.That(browser.Text, Is.StringContaining("Тариф"));
				Assert.That(browser.Text, Is.StringContaining("Баланс"));
				Assert.That(browser.Text, Is.StringContaining("Логин"));
				Assert.That(browser.Text, Is.StringContaining("ФИО"));
				var phisCl = PhysicalClient.FindFirst();
				browser.Link(Find.ByText(phisCl.Surname + " " + phisCl.Name + " " + phisCl.Patronymic)).Click();
				Thread.Sleep(400);
				Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
			}
		}

		[Test]
		public void ClientInfoTest()
		{
			ClientHelper.CreateClient(l => {
				using (var browser = Open("UserInfo/SearchUserInfo.rails?ClientCode=" + l.Id)) {
					Assert.That(browser.Text, Is.StringContaining("Город"));
					Assert.That(browser.Text, Is.StringContaining("Паспортные данные:"));
					Assert.That(browser.Text, Is.StringContaining("Адрес регистрации:"));
					Assert.That(browser.Text, Is.StringContaining("Дата регистрации:"));
					Assert.That(browser.Text, Is.StringContaining("Тариф"));
					Assert.That(browser.Text, Is.StringContaining("Платеж зарегистрировал"));
					Assert.That(browser.Text, Is.StringContaining("Дата оплаты"));
					Assert.That(browser.Text, Is.StringContaining("Сумма"));
					browser.Button(Find.ById("EditButton")).Click();
					Assert.That(browser.Text, Is.StringContaining("Регистрационные данные"));
					Assert.That(browser.Text, Is.StringContaining("Паспортные данные"));
					Assert.That(browser.Text, Is.StringContaining("Личная информация"));
					browser.Button(Find.ById("SaveButton")).Click();
					Thread.Sleep(400);
					Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
					browser.RadioButton(Find.ById("ForTariffChange")).Checked = true;
					browser.Button(Find.ById("ChangeBalanceButton")).Click();
					Thread.Sleep(400);
					Assert.That(browser.Text, Is.StringContaining("Баланс пополнен"));
				}
				return true;
			});
		}
	}
}