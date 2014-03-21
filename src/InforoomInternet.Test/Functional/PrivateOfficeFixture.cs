using System;
using System.Linq;
using System.Net;
using Billing;
using InternetInterface;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	public class PrivateOfficeFixture : SeleniumFixture
	{
		private Client client;
		private PhysicalClient physicalClient;

		[SetUp]
		public void Setup()
		{
			//нужно очищать cookie перед каждым тестом
			//если этого не делать браузер будет помнить что вход произвел клиент из предыдущего теста
			session.CreateSQLQuery("delete from Leases").ExecuteUpdate();
			var settings = new Settings(session);

			var ipAddress = IPAddress.Parse("127.0.0.1");
			var pool = session.Query<IpPool>().FirstOrDefault(i => i.Begin == ipAddress.Address);
			if (pool == null) {
				pool = new IpPool {
					Begin = ipAddress.ToBigEndian(),
					End = ipAddress.ToBigEndian() + 100000
				};
				session.Save(pool);
			}

			physicalClient = ClientHelper.PhysicalClient();
			client = physicalClient.Client;
			client.FirstLunch = true;
			client.BeginWork = DateTime.Now;
			physicalClient.Client.AddEndpoint(new ClientEndpoint(physicalClient.Client, null, null), settings);
			session.Save(new Payment(client, 1000));

			physicalClient.Password = CryptoPass.GetHashString("1234");
			session.Save(client);

			Open("PrivateOffice/IndexOffice");

			if (!IsPresent("#Login")) {
				if (IsPresent("#exitLink")) {
					Css("#exitLink").Click();
					Open("PrivateOffice/IndexOffice");
				}
				else {
					Click("Подтвердить");
					Click("Выйти");
					Open("PrivateOffice/IndexOffice");
				}
			}
			browser.FindElementByName("Login").SendKeys(client.Id.ToString());
			browser.FindElementByName("Password").SendKeys("1234");
			Css("#LogBut").Click();
		}

		[Test]
		public void Show_message()
		{
			session.Save(new MessageForClient {
				Text = "test_message_for_client",
				Client = client
			});

			Refresh();

			WaitForText("test_message_for_client");
			AssertText("test_message_for_client");
		}

		[Test]
		public void Create_end_point_if_client_dont_have()
		{
			var _switch = new NetworkSwitch();
			session.Save(_switch);
			client.Endpoints.Clear();
			client.FirstLunch = false;
			session.Save(client);
			Css("#exitLink").Click();
			var lease = new Lease {
				Ip = IPAddress.Parse("192.168.0.1"),
				Switch = _switch,
				Port = 1
			};
			session.Save(lease);

			Open("PrivateOffice/IndexOffice");
			browser.FindElementByName("Login").SendKeys(client.Id.ToString());
			browser.FindElementByName("Password").SendKeys("1234");
			Css("#LogBut").Click();

			Click("Подтвердить");

			client.Refresh();
			Assert.AreEqual(client.Endpoints.Count, 1);
			Assert.That(client.Endpoints.First().Switch, Is.EqualTo(_switch));
			Assert.That(client.Endpoints.First().Port, Is.EqualTo(1));
			session.Refresh(_switch);
			session.Refresh(lease);
			Assert.That(_switch.Name, Is.StringContaining("testStreet"));
			Assert.That(lease.Endpoint, Is.EqualTo(client.Endpoints.First()));
		}

		[Test]
		public void Activate_work_in_debt()
		{
			client.AutoUnblocked = true;
			client.Disabled = true;
			physicalClient.Balance = -100;
			session.Save(physicalClient);

			Refresh();

			Click("Подробнее...");
			Click("Активировать на 3 дня");

			AssertText("Ваш личный кабинет");
			AssertText("Услуга \"Обещанный платеж активирована\"");
		}

		[Test]
		public void PrivateOfficeTest()
		{
			session.Save(physicalClient.WriteOff(500));
			Refresh();
			AssertText("Ваш личный кабинет");
			AssertText("Номер лицевого счета для оплаты " + client.Id.ToString("00000"));
			AssertText("500");
		}

		[Test, Ignore("возможность включения/отключения интернета убрана из личного кабинета пользователя")]
		public void Deactivate_internet()
		{
			var billing = new MainBilling();

			billing.OnMethod();
			session.CreateSQLQuery("delete from internet.Appeals;").ExecuteUpdate();

			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");

			session.Refresh(client);
			Assert.That(client.Internet.ActivatedByUser, Is.False);

			billing.OnMethod();

			var appeals = Appeals.GetAllAppeal(session, client, AppealType.System);
			Assert.AreEqual(appeals.Count, 1);
			Assert.That(appeals[0].Text, Is.StringContaining("Отключена услуга Internet"));
		}

		[Test, Ignore("возможность включения/отключения интернета убрана из личного кабинета пользователя")]
		public void Diactivete_and_activete_witch_null_tariff()
		{
			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");
			Css("#client_PhysicalClient_Tariff_Id").SelectByValue(string.Empty);
			Click("Сохранить");
			Css("#internet_ActivatedByUser").Checked = true;
			Click("Сохранить");
			AssertText("Нужно выбрать тариф");
		}

		[Test]
		public void Friend_bunus_base_view_test()
		{
			Click("Бонусные программы");
			AssertText("Ваши друзья до сих пор пользуются интернетом с низкой скоростью?");

			AssertText("Оптоволоконного Интернета");
			AssertText("оформите заявку");

			Click("Подключи друга");
			AssertText("Подключи друга и получи 250 рублей на свой лицевой счёт!");
			Click("оформите заявку");
			AssertText("Заполнение данной заявки означает принятие участие в акции \"подключи друга\".");
		}

		[Test]
		public void Friend_bonus_create_test()
		{
			Open("Main/zayavka");
			AssertText("Заполнение данной заявки означает принятие участие в акции \"подключи друга\".");
			browser.FindElementById("fio").SendKeys("testFio");
			browser.FindElementById("phone_").SendKeys("8-900-900-90-90");
			browser.FindElementById("City").SendKeys("Воронеж");
			browser.FindElementById("residence").SendKeys("Студенческая");
			browser.FindElementById("House").SendKeys("12");
			browser.FindElementById("CaseHouse").SendKeys("а");
			browser.FindElementById("Apartment").SendKeys("1");
			browser.FindElementById("Entrance").SendKeys("2");
			browser.FindElementById("Floor").SendKeys("1");
			Click("Отправить");
			AssertText("Спасибо, Ваша заявка принята. Номер заявки");
			var requests = session.QueryOver<Request>().Where(r => r.FriendThisClient == client).List();
			Assert.That(requests.Count, Is.EqualTo(1));
		}

		[Test]
		public void Fist_lunch_test()
		{
			client.FirstLunch = false;
			session.Save(client);

			Open("PrivateOffice/IndexOffice");
			AssertText("Это Ваше первое посещение личного кабинета, просим подтвердить свои данные");
		}

		[Test]
		public void First_lunch_passport_data_no_valid_test()
		{
			client.FirstLunch = false;
			session.Save(client);

			Open("PrivateOffice/IndexOffice");
			Css("#PhysicalClient_PassportSeries").Clear();
			Css("#PhysicalClient_PassportSeries").SendKeys("abcd");
			Css("#PhysicalClient_PassportNumber").Clear();
			Css("#PhysicalClient_PassportNumber").SendKeys("abcd");
			Css("#PhysicalClient_Surname").Clear();
			Css("#PhysicalClient_Name").Clear();
			Css("#PhysicalClient_Patronymic").Clear();
			Click("Подтвердить");
			AssertText("Введите фамилию");
			AssertText("Введите имя");
			AssertText("Введите отчество");
			AssertText("Неправильный формат серии паспорта (4 цифры)");
			AssertText("Неправильный формат номера паспорта (6 цифр)");
		}

		[Test]
		public void First_lunch_passport_data_valid_test()
		{
			client.FirstLunch = false;
			session.Save(client);

			Open("PrivateOffice/IndexOffice");

			Css("#PhysicalClient_PassportSeries").Clear();
			Css("#PhysicalClient_PassportSeries").SendKeys("1234");
			Css("#PhysicalClient_PassportNumber").Clear();
			Css("#PhysicalClient_PassportNumber").SendKeys("123456");

			Css("#PhysicalClient_Surname").Clear();
			Css("#PhysicalClient_Surname").SendKeys("testovoi");

			Css("#PhysicalClient_Name").Clear();
			Css("#PhysicalClient_Name").SendKeys("test");

			Css("#PhysicalClient_Patronymic").Clear();
			Css("#PhysicalClient_Patronymic").SendKeys("testovich");

			Click("Подтвердить");
			AssertText("Спасибо, теперь вы можете продолжить работу");

			session.Refresh(client);
			Assert.IsTrue(client.AutoUnblocked);
			Assert.IsTrue(client.FirstLunch);
		}

		[Test, Ignore("Нужны тарифы-регионы")]
		public void Tariffs_are_not_bound_to_region_valid_test()
		{
			Open("PrivateOffice/IndexOffice");
			var region = new RegionHouse("TEST-REGION");
			session.Save(region);
			physicalClient.HouseObj = new House("st. testing", 2, region);

			Open("PrivateOffice/Services");
			session.Delete(region);

			var select = browser.FindElementByName("client.PhysicalClient.Tariff.Id");
			Assert.That(select.FindElements(By.XPath("//option")).Count, Is.EqualTo(1));
		}

		[Test, Ignore("Нужны тарифы-регионы")]
		public void Tariffs_are_bound_to_region_valid_test()
		{
			/*Open("PrivateOffice/IndexOffice");
			var region = new RegionHouse("TEST-REGION");
			var tariffs = new Tariff[3];
			for (var i = 0; i < 3; i++) {
				tariffs[i] = new Tariff("tariff" + i, 100);
				tariffs[i].CanUseForSelfConfigure = true;

				session.Save(tariffs[i]);
				region.Tariffs.Add(tariffs[i]);
			}
			session.Save(region);
			physicalClient.HouseObj = new House("st. testing", 2, region);
			Open("PrivateOffice/Services");
			session.Delete(region);
			foreach (Tariff t in tariffs) {
				session.Delete(t);
			}
			var select = browser.FindElementByName("client.PhysicalClient.Tariff.Id");
			Assert.That(select.FindElements(By.XPath("//option")).Count, Is.EqualTo(tariffs.Length + 1));*/
		}
	}
}
