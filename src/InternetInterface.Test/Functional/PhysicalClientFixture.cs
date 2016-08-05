using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class PhysicalClientFixture : AcceptanceFixture
	{
		[Test]
		public void NoOrderForPhysicalClient()
		{
			var reg = session.Query<RegionHouse>().First(i => i.Name == "Воронеж");
			var zone = session.Query<Zone>().First(i => i.Region == reg);
			session.Save(zone);
			var sw = new NetworkSwitch("ClientTestSwitch", zone);
			session.Save(sw);

			var url = string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing={1}", client.Id, false);
			Open(url);

			Css("input[name='EditConnectFlag'] + button").Click();
			Css("input#Port").SendKeys("1");
			Css("#SelectSwitches").SelectByValue(sw.Id.ToString());
			Css("input#SaveConnectionBtn").Click();
			var order = session.Get<Order>(client.Id);
			Assert.Null(order);
			var endpoint = session.Query<ClientEndpoint>().First(i => i.Client == client && !i.Disabled);
			Assert.NotNull(endpoint);
			Assert.False(browser.PageSource.Contains("class=\"err\""));
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
			Open(client.Redirect());
			AssertText("Информация по клиенту");
			Click("Редактировать");
			AssertText("Личная информация");
			Css("#client_Tariff_Id").Select("Тариф для тестирования");
			Css("#SaveButton").Click();
			AssertText("Данные изменены");
		}

		[Test, Ignore("Аренда ТВ-приставки исключена из данного интерфейса; задача №28785")]
		public void Activate_iptv_box_rent()
		{
			Open(client.Redirect());
			var el = SafeSelectService("Аренда приставки");
			Css(el, "#clientService_Model").SendKeys("IP STB Aminet");
			Css(el, "#clientService_SerialNumber").SendKeys("748644654");
			Click(el, "Активировать");

			AssertText("Услуга \"Аренда приставки\" активирована");

			el = SafeSelectService("Аренда приставки");
			Click(el, "Деактивировать");
			AssertText("Услуга \"Аренда приставки\" деактивирована");
		}

		[Test, Ignore("Аренда ТВ-приставки исключена из данного интерфейса; задача №28785")]
		public void Print_rent_contract()
		{
			var hardware = session.Query<RentableHardware>().FirstOrDefault(h => h.Name == "Коммутатор");
			if (hardware == null) {
				hardware = new RentableHardware(150, "Коммутатор");
				session.Save(hardware);
			}
			Open(client.Redirect());
			var el = SafeSelectService("Аренда оборудования");
			Css(el, "#startDate").SendKeys(DateTime.Today.ToShortDateString());
			Css(el, "#clientService_Model").SendKeys("DES-1005A/C1");
			Css(el, "#clientService_SerialNumber").SendKeys("748644654");
			Css(el, "#clientService_RentableHardware_Id").SelectByValue(hardware.Id.ToString());
			Click(el, "Активировать");

			AssertText("Услуга \"Аренда оборудования\" активирована");
			Click("Аренда оборудования - Коммутатор");
			AssertText("Акт возврата оборудование");
			Click("Договор и Акт приема-передачи");
			Click("Сформировать");
			AssertText("ДОГОВОР-ОФЕРТА аренды телекоммуникационного оборудования для физических лиц");
		}
	}
}