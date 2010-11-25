using System;
using System.Linq;
using System.Threading;
using InternetInterface.Models;
using InternetInterfaceFixture.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterfaceFixture.Functional
{
	[TestFixture]
	class SearchFixture : WatinFixture
	{
		[Test]
		public void SearchTest()
		{
			using (var browser = Open("Search/SearchUsers.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("Поиск пользователей"));
				Assert.That(browser.Text, Is.StringContaining("Введите текст для поиска:"));
				Assert.That(browser.Text, Is.StringContaining("Автоматически"));
				Assert.That(browser.Text, Is.StringContaining("Тариф"));
				Assert.That(browser.Text, Is.StringContaining("Все"));
				Assert.That(browser.Text, Is.StringContaining("Кем загистрирован"));
				Assert.That(browser.Text, Is.StringContaining("Назначено на бригаду"));
				browser.Button(Find.ById("SearchButton")).Click();
				Thread.Sleep(400);
				Assert.That(browser.Text, Is.StringContaining("Город"));
				Assert.That(browser.Text, Is.StringContaining("Тариф"));
				Assert.That(browser.Text, Is.StringContaining("Баланс"));
				Assert.That(browser.Text, Is.StringContaining("Логин"));
				Assert.That(browser.Text, Is.StringContaining("ФИО"));
				browser.Link(Find.ByText("Путин Владимир Владимирович")).Click();
				Thread.Sleep(400);
				Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
			}
		}
		[Test]
		public void ClientInfoTest()
		{
			using (var browser = Open("UserInfo/SearchUserInfo.rails?ClientCode=2"))
			{
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
				Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
				browser.RadioButton(Find.ById("ForTariffChange")).Checked = true;
				browser.Button(Find.ById("ChangeBalanceButton")).Click();
				Assert.That(browser.Text, Is.StringContaining("Баланс пополнен"));
			}
		}
	}
	}