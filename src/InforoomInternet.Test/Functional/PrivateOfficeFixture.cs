using System;
using System.Linq;
using InternetInterface;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

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

		[Test, Ignore("Функционал временно отключен")]
		public void Deactivate_internet()
		{
			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");

			session.Refresh(client);
			Assert.That(client.Internet.ActivatedByUser, Is.False);
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
	}
}