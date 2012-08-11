﻿using System;
using InternetInterface;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

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
			Click("Управление услугами");
			Css("#internet_ActivatedByUser").Click();
			Click("Сохранить");

			session.Refresh(client);
			Assert.That(client.Internet.ActivatedByUser, Is.False);
		}
	}
}