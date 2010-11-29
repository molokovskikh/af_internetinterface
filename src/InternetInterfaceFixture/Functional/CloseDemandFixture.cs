using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterfaceFixture.Helpers;
using NHibernate.Criterion;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterfaceFixture.Functional
{
	[TestFixture]
	class CloseDemandFixture : WatinFixture
	{
		[Test]
		public void SearchTest()
		{
			var client = PhisicalClients.FindFirst();
			client.Connected = false;
			client.HasConnected = null;
			client.UpdateAndFlush();
			InithializeContent.partner = Partner.FindAllByProperty("Login", "zolotarev")[0];
			var RC = new RequestsConnection
			{
				BrigadNumber = Brigad.FindAll(DetachedCriteria.For(typeof(Brigad))
												.Add(Expression.Eq("PartnerID", InithializeContent.partner)))[0],
				ClientID = client,
				ManagerID = InithializeContent.partner,
				RegDate = DateTime.Now
			};
			RC.SaveAndFlush();
			Thread.Sleep(2000);
			using (var browser = Open("Search/SearchBy.rails?CloseDemand=true"))
			{
				/*Assert.That(browser.Text, Is.StringContaining("Подключить"));
				Assert.That(browser.Text, Is.StringContaining("Свич"));
				Assert.That(browser.Text, Is.StringContaining("Свич"));
				Assert.That(browser.Text, Is.StringContaining("Порт"));*/
				browser.CheckBox(Find.ById("CliseDemandCheck" + client.Id)).Checked = true;
				browser.TextField(Find.ById("Port" + client.Id)).AppendText("20");
				browser.SelectList(Find.ById("SelectSwitches" + client.Id)).SelectByValue("2");
				browser.Button(Find.ById("CliseDemandButton")).Click();
				//Assert.That(browser.Text, Is.StringContaining("Заявки закрыты"));
			}
			//RC.DeleteAndFlush();
			var clientAndPoint = ClientEndpoints.FindAll(DetachedCriteria.For(typeof (ClientEndpoints))
			                                             	.Add(Expression.Eq("Port", 20))
			                                             	.Add(Expression.Eq("Switch", NetworkSwitches.Find((uint)2))))[0];
			clientAndPoint.DeleteAndFlush();
		}
	}
}