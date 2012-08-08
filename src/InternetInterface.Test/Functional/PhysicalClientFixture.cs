using System.Linq;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	public class PhysicalClientFixture : global::Test.Support.Web.WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.Client();
			var internet = session.Query<Internet>().First();
			var iptv = session.Query<IpTv>().First();
			client.ClientServices.Add(new ClientService(client, internet));
			client.ClientServices.Add(new ClientService(client, iptv));
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
			Click("Активировать");
			AssertText("Услуга \"Аренда приставки\" активирована");
			Click("Управление услугами");
			Click("Аренда приставки");
			Click("Деактивировать");
			AssertText("Услуга \"Аренда приставки\" деактивирована");
		}
	}
}