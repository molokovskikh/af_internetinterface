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
		private IPAddress ipAddress;

		[SetUp]
		public void Setup()
		{
			//нужно очищать cookie перед каждым тестом
			//если этого не делать браузер будет помнить что вход произвел клиент из предыдущего теста
			session.CreateSQLQuery("delete from Leases").ExecuteUpdate();
			var settings = new Settings(session);

			ipAddress = IPAddress.Parse("192.168.1.1");
			var pool = session.Query<IpPool>().FirstOrDefault(i => i.Begin == ipAddress.ToBigEndian());
			if (pool == null) {
				pool = new IpPool {
					Begin = IPAddress.Parse("192.168.1.1").ToBigEndian(),
					End = IPAddress.Parse("192.168.1.100").ToBigEndian(),
				};
				session.Save(pool);
			}

			physicalClient = ClientHelper.PhysicalClient(session);
			client = physicalClient.Client;
			client.FirstLaunch = true;
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
			client.Endpoints.Clear();
			client.FirstLaunch = false;
			session.Save(client);
			var lease = CreateLease();

			Css("#exitLink").Click();
			Open("PrivateOffice/IndexOffice");
			browser.FindElementByName("Login").SendKeys(client.Id.ToString());
			browser.FindElementByName("Password").SendKeys("1234");
			Css("#LogBut").Click();

			Click("Подтвердить");

			client.Refresh();
			Assert.AreEqual(client.Endpoints.Count, 1);
			Assert.That(client.Endpoints.First().Switch, Is.EqualTo(lease.Switch));
			Assert.That(client.Endpoints.First().Port, Is.EqualTo(1));
			session.Refresh(lease.Switch);
			session.Refresh(lease);
			Assert.That(lease.Switch.Name, Is.StringContaining("testStreet"));
			Assert.That(lease.Endpoint, Is.EqualTo(client.Endpoints.First()));
		}

		private Lease CreateLease()
		{
			var networkSwitch = new NetworkSwitch();
			session.Save(networkSwitch);
			var lease = new Lease {
				Ip = ipAddress,
				Switch = networkSwitch,
				Port = 1
			};
			session.Save(lease);
			return lease;
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
			Click("Активировать");

			AssertText("Ваш личный кабинет");
			AssertText("Услуга \"Обещанный платеж\" активирована");
		}

		[Test]
		public void PrivateOfficeTest()
		{
			session.Save(physicalClient.WriteOff(500));
			Refresh();
			AssertText("Ваш личный кабинет");
			AssertText("Номер лицевого счета для оплаты " + client.Id.ToString("00000"));
			AssertText("Баланс составляет -400,00");
			AssertText("500");
		}

		[Test]
		public void Show_balance_info()
		{
			Css(".balance-info").Click();
			AssertText("Подробная информация о балансе");
		}

		[Test, Ignore("возможность включения/отключения интернета убрана из личного кабинета пользователя")]
		public void Deactivate_internet()
		{
			var billing = new MainBilling();

			billing.ProcessPayments();
			session.CreateSQLQuery("delete from internet.Appeals;").ExecuteUpdate();

			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");

			session.Refresh(client);
			Assert.That(client.Internet.ActivatedByUser, Is.False);

			billing.ProcessPayments();

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
			Css("#request_ApplicantName").SendKeys("testFio");
			Css("#request_ApplicantPhoneNumber").SendKeys("8-900-900-90-90");
			Css("#request_City").SendKeys("Воронеж");
			Css("#request_Street").SendKeys("Студенческая");
			Css("#request_House").SendKeys("12");
			Css("#request_CaseHouse").SendKeys("а");
			Css("#request_Apartment").SendKeys("1");
			Css("#request_Entrance").SendKeys("2");
			Css("#request_Floor").SendKeys("1");
			Click("Отправить");
			AssertText("Спасибо, Ваша заявка принята. Номер заявки");
			var requests = session.QueryOver<Request>().Where(r => r.FriendThisClient == client).List();
			Assert.That(requests.Count, Is.EqualTo(1));
		}

		[Test]
		public void Fist_launch_test()
		{
			CreateLease();
			client.FirstLaunch = false;
			session.Save(client);

			Open("PrivateOffice/IndexOffice");
			AssertText("Это Ваше первое посещение личного кабинета, просим подтвердить свои данные");
		}

		[Test]
		public void First_launch_passport_data_no_valid_test()
		{
			CreateLease();
			client.FirstLaunch = false;
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
		public void First_launch_passport_data_valid_test()
		{
			CreateLease();
			client.FirstLaunch = false;
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
			Assert.IsTrue(client.FirstLaunch);
		}

		[Test]
		public void Blocked_for_repair()
		{
			client.SetStatus(StatusType.BlockedForRepair, session);
			Open("PrivateOffice/IndexOffice");
			AssertText("Доступ в интернет заблокирован из-за проведения работ по сервисной заявке");
			Click("Продолжить");
			AssertText("Работа возобновлена");
		}
	}
}
