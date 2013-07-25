using System.Linq;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	public class PhysicalClientFixture : global::Test.Support.Web.WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.Client();
			session.Save(client);
		}

		[Test]
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
			var el = Css("input[value='9']");
			((Form)el.Parent).Submit();
			AssertText("Услуга \"Аренда приставки\" активирована");
			Click("Управление услугами");
			Click("Аренда приставки");
			Click("Деактивировать");
			AssertText("Услуга \"Аренда приставки\" деактивирована");
		}

		[Test]
		public void NoOrderForPhysicalClient()
		{
			Open("UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
			Css("#Port").AppendText("1");
			var el = Css("#Submit2");
			el.Click();
			var order = session.Get<Order>(client.Id);
			Assert.Null(order);
		}
	}
}