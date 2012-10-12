using System;
using System.Linq;
using Billing;
using InternetInterface;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InforoomInternet.Test.Functional
{
	public class PrivateOfficeFixture : global::Test.Support.Web.WatinFixture2
	{
		private Client client;
		private PhysicalClient physicalClient;

		[SetUp]
		public void Setup()
		{
			session.CreateSQLQuery("delete from Leases").ExecuteUpdate();

			physicalClient = ClientHelper.PhysicalClient();
			client = physicalClient.Client;
			client.FirstLunch = true;
			client.BeginWork = DateTime.Now;
			physicalClient.Client.Endpoints.Add(new ClientEndpoint(physicalClient.Client, null, null));
			session.Save(new Payment(client, 1000));

			physicalClient.Password = CryptoPass.GetHashString("1234");
			session.Save(client);

			Open("PrivateOffice/IndexOffice");
			var exit = Css("#exitLink");
			if (exit != null) {
				exit.Click();
				Open("PrivateOffice/IndexOffice");
			}
			browser.TextField("Login").AppendText(client.Id.ToString());
			browser.TextField("Password").AppendText("1234");
			browser.Button("LogBut").Click();
		}

		[Test]
		public void Create_end_point_if_client_dont_have()
		{
			var _switch = new NetworkSwitch();
			session.Save(_switch);
			client.Endpoints.Clear();
			session.SaveOrUpdate(client);
			Css("#exitLink").Click();
			var lease = new Lease {
				Ip = Convert.ToUInt32(NetworkSwitch.SetProgramIp("192.168.0.1")),
				Switch = _switch,
				Port = 1
			};
			session.Save(lease);
			Flush();
			Open("PrivateOffice/IndexOffice");
			browser.TextField("Login").AppendText(client.Id.ToString());
			browser.TextField("Password").AppendText("1234");
			browser.Button("LogBut").Click();
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
			Click("Разблокировать");

			AssertText("Ваш личный кабинет");
			AssertText("Услуга \"Обещанный платеж активирована\"");
		}

		[Test]
		public void Show_message()
		{
			session.Save(new MessageForClient {
				Text = "test_message_for_client",
				Client = client
			});

			Refresh();
			AssertText("test_message_for_client");
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

		[Test]
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
			Assert.That(appeals[0].Text, Is.StringContaining("Отключена услуна Internet"));
		}

		[Test]
		public void Diactivete_and_activete_witch_null_tariff()
		{
			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");
			browser.SelectList("client_PhysicalClient_Tariff_Id").SelectByValue(string.Empty);
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
			Assert.IsNotNull(browser.Link(Find.ByText("Оптоволоконного Интернета")));
			Assert.IsNotNull(browser.Link(Find.ByText("оформите заявку")));
			Click("Подключи друга");
			AssertText("Подключи друга и получи 250 рублей на свой лицевой счёт!");
			Click("оформите заявку");
			AssertText("Заполнение данной заявки означает принятие участие в акции \"подключи друга\".");
		}

		[Test]
		public void Freind_bonus_create_test()
		{
			Open("Main/zayavka");
			AssertText("Заполнение данной заявки означает принятие участие в акции \"подключи друга\".");
			browser.TextField("fio").AppendText("testFio");
			browser.TextField("phone_").AppendText("8-900-900-90-90");
			browser.TextField("City").AppendText("Воронеж");
			browser.TextField("residence").AppendText("Студенческая");
			browser.TextField("House").AppendText("12");
			browser.TextField("CaseHouse").AppendText("а");
			browser.TextField("Apartment").AppendText("1");
			browser.TextField("Entrance").AppendText("2");
			browser.TextField("Floor").AppendText("1");
			Click("Отправить");
			AssertText("Спасибо, Ваша заявка принята. Номер заявки");
			var requests = session.QueryOver<Request>().Where(r => r.FriendThisClient == client).List();
			Assert.That(requests.Count, Is.EqualTo(1));
		}

		[Test]
		public void Fist_lunch_test()
		{
			client.FirstLunch = false;
			session.SaveOrUpdate(client);
			Flush();
			Open("PrivateOffice/IndexOffice");
			AssertText("Это Ваше первое посещение личного кабинета, просим подтвердить свои данные");
		}

		[Test]
		public void First_lunch_passport_data_no_valid_test()
		{
			client.FirstLunch = false;
			session.SaveOrUpdate(client);
			Flush();
			Open("PrivateOffice/IndexOffice");
			browser.TextField("PhysicalClient_PassportSeries").AppendText("abcd");
			browser.TextField("PhysicalClient_PassportNumber").AppendText("abcd");
			browser.TextField("PhysicalClient_Surname").Clear();
			browser.TextField("PhysicalClient_Name").Clear();
			browser.TextField("PhysicalClient_Patronymic").Clear();
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
			session.SaveOrUpdate(client);
			Flush();
			Open("PrivateOffice/IndexOffice");
			browser.TextField("PhysicalClient_PassportSeries").Value = "1234";
			browser.TextField("PhysicalClient_PassportNumber").Value = "123456";
			browser.TextField("PhysicalClient_Surname").AppendText("testovoi");
			browser.TextField("PhysicalClient_Name").AppendText("test");
			browser.TextField("PhysicalClient_Patronymic").AppendText("testovich");
			Click("Подтвердить");
			AssertText("Спасибо, теперь вы можете продолжить работу");

			session.Refresh(client);
			Assert.IsTrue(client.AutoUnblocked);
			Assert.IsTrue(client.Disabled);
			Assert.IsTrue(client.FirstLunch);
		}
	}
}