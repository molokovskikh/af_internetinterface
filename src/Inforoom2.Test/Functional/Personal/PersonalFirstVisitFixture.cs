﻿using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	public class PersonalFirstVisitFixture : PersonalFixture
	{
		[Test]
		public void FirstVisit()
		{
			var passportSeries = "1234";
			var passportNumber = "123456";
			var passportResidention = "УФМС россии по гор. Воронежу, по райнону Северный"; // "Паспортно-визовое отделение по району северный гор. Воронежа";
			var passportAddress = "г. Борисоглебск, улица ленина, 20"; //"г. Воронеж, студенческая ул, д12";

			var clientMark = ClientCreateHelper.ClientMark.unpluggedClient.GetDescription();
			var leaseList = DbSession.Query<Lease>().ToList();
			leaseList.RemoveAt(0);
			DbSession.DeleteEach(leaseList);
			DbSession.Flush();
			var firstLease = DbSession.Query<Lease>().FirstOrDefault();
			var lease = new Lease() {
				Ip = new IPAddress(firstLease.Ip.Address + 10),
				LeaseBegin = firstLease.LeaseBegin,
				LeasedTo = firstLease.LeasedTo,
				LeaseEnd = firstLease.LeaseEnd,
				Switch = firstLease.Switch,
				Port = firstLease.Port + 1
			};
			DbSession.Save(lease);

			Client = DbSession.Query<Client>().ToList().First(i => i.Comment == clientMark);
			Client.Disabled = true;
			Client.Endpoints.RemoveEach(Client.Endpoints);
      DbSession.Save(Client);
			DbSession.Flush();
			var internet = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			var iptv = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			Assert.That(Client.Lunched, Is.False);
			Assert.That(Client.Endpoints.Count, Is.EqualTo(0));
			Assert.That(internet.IsActivated, Is.False);
			Assert.That(iptv.IsActivated, Is.False);

			LoginForClient(Client);
			//Появляется варнинг, первое, что необходимо сделать - ввести паспортные данные.
			AssertText("Серия паспорта");

			var series = browser.FindElementByCssSelector("input[name='physicalClient.PassportSeries']");
			series.SendKeys(passportSeries);

			var number = browser.FindElementByCssSelector("input[name='physicalClient.PassportNumber']");
			number.SendKeys(passportNumber);

			var date = browser.FindElementByCssSelector("input[name='physicalClient.PassportDate']");
			date.Click();
			var popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();

			date = browser.FindElementByCssSelector("input[name='physicalClient.BirthDate']");
			date.Click();
			popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			//date.SendKeys("18.12.2014");

			var residention = browser.FindElementByCssSelector("input[name='physicalClient.PassportResidention']");
			residention.SendKeys(passportResidention);

			var address = browser.FindElementByCssSelector("input[name='physicalClient.RegistrationAddress']");
			address.SendKeys(passportAddress);

			var button = browser.FindElementByCssSelector(".right-block .button");
			button.Click();

			RunBillingProcessWriteoffs();
			RunBillingProcessWriteoffs();
			RunBillingProcessClientEndpointSwitcher();

			AssertText("успешно");
			DbSession.Flush();
			DbSession.Refresh(Client);
			DbSession.Refresh(Client.PhysicalClient);
			var client = Client;
			internet = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			iptv = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			//Проверяем объекты
			Assert.That(client.Lunched, Is.True);
			Assert.That(client.Endpoints.Count(s => !s.Disabled), Is.EqualTo(1));
			Assert.That(client.Endpoints.First(s => !s.Disabled).PackageId, Is.EqualTo(client.Plan.PackageSpeed.PackageId), "PackageId должен равняться PackageId тарифа.");
			Assert.That(internet.IsActivated, Is.True);
			Assert.That(iptv.IsActivated, Is.True);

			//Проверям значения введенных данных
			Assert.That(client.PhysicalClient.PassportNumber, Is.EqualTo(passportNumber));
			Assert.That(client.PhysicalClient.RegistrationAddress, Is.EqualTo(passportAddress));
			Assert.That(client.PhysicalClient.PassportResidention, Is.EqualTo(passportResidention));
			Assert.That(client.PhysicalClient.PassportSeries, Is.EqualTo(passportSeries));
			Assert.That(client.PhysicalClient.PassportDate, Is.Not.EqualTo(DateTime.MinValue));
			Assert.That(client.PhysicalClient.BirthDate, Is.Not.EqualTo(DateTime.MinValue));
		}

		[Test]
		public void FirstVisitWithBusyPort()
		{
			var passportSeries = "1234";
			var passportNumber = "123456";
			var passportResidention = "УФМС россии по гор. Воронежу, по райнону Северный"; // "Паспортно-визовое отделение по району северный гор. Воронежа";
			var passportAddress = "г. Борисоглебск, улица ленина, 20"; //"г. Воронеж, студенческая ул, д12";

			var clientMark = ClientCreateHelper.ClientMark.unpluggedClient.GetDescription();
			var leaseList = DbSession.Query<Lease>().ToList();
			leaseList.RemoveAt(0);
			DbSession.DeleteEach(leaseList);
			DbSession.Flush();
			var firstLease = DbSession.Query<Lease>().FirstOrDefault();
			var lease = new Lease() {
				Ip = new IPAddress(firstLease.Ip.Address + 10), LeaseBegin = firstLease.LeaseBegin,
				LeasedTo = firstLease.LeasedTo, LeaseEnd = firstLease.LeaseEnd, Switch = firstLease.Switch, Port = firstLease.Port
			};
			DbSession.Save(lease);
			Client = DbSession.Query<Client>().ToList().First(i => i.Comment == clientMark);
			Client.Endpoints.RemoveEach(Client.Endpoints);
			Client.Disabled = true;
			DbSession.Save(Client);
			DbSession.Flush();
			var internet = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			var iptv = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			Assert.That(Client.Lunched, Is.False);
			Assert.That(Client.Endpoints.Count, Is.EqualTo(0));
			Assert.That(internet.IsActivated, Is.False);
			Assert.That(iptv.IsActivated, Is.False);

			LoginForClient(Client);
			//Появляется варнинг, первое, что необходимо сделать - ввести паспортные данные.
			AssertText("Серия паспорта");

			var series = browser.FindElementByCssSelector("input[name='physicalClient.PassportSeries']");
			series.SendKeys(passportSeries);

			var number = browser.FindElementByCssSelector("input[name='physicalClient.PassportNumber']");
			number.SendKeys(passportNumber);

			var date = browser.FindElementByCssSelector("input[name='physicalClient.PassportDate']");
			date.Click();
			var popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();

			date = browser.FindElementByCssSelector("input[name='physicalClient.BirthDate']");
			date.Click();
			popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			//date.SendKeys("18.12.2014");

			var residention = browser.FindElementByCssSelector("input[name='physicalClient.PassportResidention']");
			residention.SendKeys(passportResidention);

			var address = browser.FindElementByCssSelector("input[name='physicalClient.RegistrationAddress']");
			address.SendKeys(passportAddress);

			var button = browser.FindElementByCssSelector(".right-block .button");
			button.Click();

			RunBillingProcessWriteoffs();
			RunBillingProcessWriteoffs();
			RunBillingProcessClientEndpointSwitcher();

			AssertText("Ошибка: точка подключения не задана");
			AssertText("успешно");
			DbSession.Clear();
			DbSession.Refresh(Client);
			DbSession.Refresh(Client.PhysicalClient);
			var client = DbSession.Query<Client>().ToList().First(i => i.Comment == clientMark);
			internet = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			iptv = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			//Проверяем объекты
			Assert.That(client.Lunched, Is.True);
			Assert.That(client.Endpoints.Count, Is.EqualTo(0));
			Assert.That(internet.IsActivated, Is.False);
			Assert.That(iptv.IsActivated, Is.False);

			//Проверям значения введенных данных
			Assert.That(client.PhysicalClient.PassportNumber, Is.EqualTo(passportNumber));
			Assert.That(client.PhysicalClient.RegistrationAddress, Is.EqualTo(passportAddress));
			Assert.That(client.PhysicalClient.PassportResidention, Is.EqualTo(passportResidention));
			Assert.That(client.PhysicalClient.PassportSeries, Is.EqualTo(passportSeries));
			Assert.That(client.PhysicalClient.PassportDate, Is.Not.EqualTo(DateTime.MinValue));
			Assert.That(client.PhysicalClient.BirthDate, Is.Not.EqualTo(DateTime.MinValue));
		}
	}
}