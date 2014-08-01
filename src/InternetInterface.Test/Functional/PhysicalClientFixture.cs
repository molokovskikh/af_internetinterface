using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
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
			Open("UserInfo/SearchUserInfo?filter.ClientCode=" + client.Id);
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
			Open("UserInfo/SearchUserInfo?filter.ClientCode=" + client.Id);
			var el = SafeSelectService("Аренда приставки");
			Css(el, "#clientService_Model").SendKeys("IP STB Aminet");
			Css(el, "#clientService_SerialNumber").SendKeys("748644654");
			Click(el, "Активировать");

			AssertText("Услуга \"Аренда приставки\" активирована");

			el = SafeSelectService("Аренда приставки");
			Click(el, "Деактивировать");
			AssertText("Услуга \"Аренда приставки\" деактивирована");
		}

		[Test]
		public void Print_rent_contract()
		{
			var hardware = session.Query<RentableHardware>().FirstOrDefault(h => h.Name == "Коммутатор");
			if (hardware == null) {
				hardware = new RentableHardware(150, "Коммутатор");
				session.Save(hardware);
			}
			Open("UserInfo/SearchUserInfo?filter.ClientCode=" + client.Id);
			Click("Управление услугами");
			var el = browser.FindElementByCssSelector("input[name='serviceId'][value='12']");
			var form = el.FindElement(By.XPath(".."));
			if (!Css(form, "#clientService_Model").Displayed)
				Click("Аренда оборудования");
			Css(form, "#startDate").SendKeys(DateTime.Today.ToShortDateString());
			Css(form, "#clientService_Model").SendKeys("DES-1005A/C1");
			Css(form, "#clientService_SerialNumber").SendKeys("748644654");
			Css(form, "#clientService_RentableHardware_Id").SelectByValue(hardware.Id.ToString());
			form.Submit();
			AssertText("Услуга \"Аренда оборудования\" активирована");
			Click("Аренда оборудования - Коммутатор");
			AssertText("Акт возврата оборудование");
			Click("Договор и Акт приема-передачи");
			Click("Сформировать");
			AssertText("ДОГОВОР-ОФЕРТА аренды телекоммуникационного оборудования для физических лиц");
		}
	}
}