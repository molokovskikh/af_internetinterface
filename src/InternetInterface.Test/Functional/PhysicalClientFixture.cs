using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class PhysicalClientFixture : AcceptanceFixture
	{
		[Test]
		public void NoOrderForPhysicalClient()
		{
			Css("input[name='EditConnectFlag'] + button").Click();
			Css("input#Port").SendKeys("1");
			Css("input#Submit2").Click();
			var order = session.Get<Order>(client.Id);
			Assert.Null(order);
		}

		[Test]
		public void Calendar_in_begin_date()
		{
			ClickLink("Статистика работы");
			Assert.NotNull(Css("input.hasDatepicker#beginDate"));
		}

		[Test, Ignore("Клиента без тарифа не должно существовать")]
		public void Edit_tariff_for_client_without_tariff()
		{
			Open("UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
			AssertText("Информация по клиенту");
			Click("Редактировать");
			AssertText("Личная информация");
			Css("#client_Tariff_Id").Select("Тариф для тестирования");
			Css("#SaveButton").Click();
			AssertText("Данные изменены");
		}

		[Test]
		public void Activate_iptv_box_rent()
		{
			Open("UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
			Click("Управление услугами");
			Click("Аренда приставки");
			var el = browser.FindElementByCssSelector("input[value='9']");
			el.FindElement(By.XPath("..")).Submit();
			AssertText("Услуга \"Аренда приставки\" активирована");
			Click("Управление услугами");
			Click("Аренда приставки");
			Click("Деактивировать");
			AssertText("Услуга \"Аренда приставки\" деактивирована");
		}
	}
}