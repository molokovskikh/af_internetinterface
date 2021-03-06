﻿using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class CreatePlanFixture : ClientActionsFixture
	{
		[Test, Description("Сохранение приватного сообщения для случайного клиента")]
		public void SavePrivateMessage()
		{
			var client = DbSession.Query<Client>().FirstOrDefault();
			Assert.IsNotNull(client, "\nНи один клиент не найден!");

			Open("Client/InfoPhysical/" + client.Id);
			Css("#PrivateMsgBtn").Click();
			ClosePreviousTab();

			AssertText(client.PhysicalClient.FullName);
			WaitForCss("[name='privateMessage.Text']", 10);
            Css("#privateMessage_Text").SendKeys("Тестовое сообщение");
			Css("#privateMessage_EndDate").SendKeys(DateTime.Now.ToShortDateString());
			// возникает ошибка бес "скликивания" по форме, 
			// т.к. пенель календаря заслоняет следующее поле 
			Css(".page-container").Click();
			Css("#privateMessage_Enabled").Click();
			Css("#SaveMsgBtn").Click();
			AssertText("Приватное сообщение успешно сохранено!");

			var msg = DbSession.Query<PrivateMessage>().FirstOrDefault(pm => pm.Client == client);
			Assert.IsNotNull(msg, "\nСообщение не найдено!");
			Assert.IsTrue(msg.Text == "Тестовое сообщение");
			Assert.IsTrue(msg.EndDate.Date == DateTime.Now.Date);
			Assert.IsTrue(msg.RegDate.Date == DateTime.Now.Date);
			Assert.IsTrue(msg.Enabled);
			Assert.IsTrue(msg.Registrator == Employee);
		}
	}
}