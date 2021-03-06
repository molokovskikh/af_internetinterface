﻿using System.Linq;
using System.Net;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional.Inforoom2
{

	/// <summary>
	/// Фикстура для действий, которые не относятся к какому-либо конкретному контроллеру
	/// </summary>
	public class BaseControllerFixture : BaseFixture
	{
		[Test, Description("Проверка авторизации клиента, используя его IP адрес")]
		public void CheckNetworkLogin()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			NetworkLoginForClient(client);
			Open("/");
			AssertText(client.Name);
			var cookie = GetCookie("networkClient");
			Assert.That(cookie, Is.EqualTo("true"), "У клиента нет куки залогиненого через сеть клиента");
		}

		[Test, Description("Проверка определения города")]
		public void CitySelectTest()
		{
			SetCookie("userCity", null);
			Open();
			AssertText("ВЫБЕРИТЕ ГОРОД");
			var links = browser.FindElementsByCssSelector("#CityWindow .cities a");
			links[1].Click();
			var userCity = GetCookie("userCity");
			Assert.That(userCity, Is.EqualTo("Борисоглебск"));
		}


		[Test, Description("Проверка смены города")]
		public void ChangeCity()
		{
			Open("");
			browser.FindElementByCssSelector(".arrow").Click();
			var oldcity = browser.FindElementByCssSelector(".city .name").Text;
			var clickCity = browser.FindElementByCssSelector(".cities a");
			var clickedText = clickCity.Text;

			clickCity.Click();

			Assert.That(oldcity, Is.Not.StringContaining(clickedText), "Выбранный город не должен быть равен изначальному");
			var name = browser.FindElementByCssSelector(".city .name").Text;
			Assert.That(name, Does.Contain(clickedText), "Изначальный город должен поменяться");
		}

		[Test(Description = "Выбор акционного тарифа")]
		public void PromotionalPlan()
		{
			Open("");
			browser.FindElementByCssSelector(".main-offer img").Click();
			AssertText("Заявка на подключение");
			browser.FindElementByCssSelector("div.city").Click();
			browser.FindElementByLinkText("Борисоглебск (частный сектор)").Click();
			var selectedValue = browser.FindElementByCssSelector("#clientRequest_Plan.rounded option[selected='selected']");
			Assert.That(selectedValue.Text, Is.EqualTo("Народный"), "В поле тариф должен быть выбран акционный тариф");
			//Должна быть одна зеленая галочка,при поиске одного элемента вероятно выдаст ошибку,в данном случае вернет пустой массив.
			var greenElements = browser.FindElementsByCssSelector(".success .icon");
			Assert.That(greenElements.Count, Is.EqualTo(1), "Зеленая галочка должен появиться на странице");
		}
	}
}