using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class HouseMapFixture : WatinFixture2
	{
		[Test]
		public void HouseMap()
		{
			using (new SessionScope())
			{
				using (var browser = Open("HouseMap/FindHouse.rails"))
				{
					browser.Button("FindHouseButton").Click();
					Assert.Greater(browser.Table("find_result_table").TableRows.Count, 0);
					browser.Link("huise_link_0").Click();
					//browser.Element(Find.ByName("EnCdsfount"));
					if (browser.Element(Find.ByName("EnCount")).Exists)
					{
						browser.TextField("EnCount").AppendText("4");
						browser.TextField("ApCount").AppendText("20");
						browser.TextField("CompetitorCount").AppendText("10");
						browser.Link("naznach_link").Click();
					}
					Assert.IsTrue(browser.Text.Contains("TV"));
					Assert.IsTrue(browser.Text.Contains("INT"));
					Assert.IsTrue(browser.Text.Contains("20"));

				}
			}
		}

		[Test]
		public void FindHouseTest()
		{
			using (var browser = Open("HouseMap/FindHouse.rails"))
			{
				browser.Button("FindHouseButton").Click();
				Assert.Greater(browser.Table("find_result_table").TableRows.Count, 0);
				browser.Link("huise_link_0").Click();
			}
		}

		private void CreateRequestForApartment()
		{
			browser = Open("HouseMap/FindHouse.rails");
			browser.Button("FindHouseButton").Click();
			Assert.Greater(browser.Table("find_result_table").TableRows.Count, 0);
			browser.Link(Find.ByClass("houses_links")).Click();
			//browser.Link("huise_link_0").Click();
			browser.Link(Find.ByClass("apartment_link")).Click();
			browser.Button("CreateRequest").Click();
			browser.TextField("request_ApplicantName").AppendText("testReq test test");
			browser.TextField("request_ApplicantPhoneNumber").AppendText("8-900-900-90-90");
			browser.TextField("request_Entrance").AppendText("2");
			browser.TextField("request_Floor").AppendText("2");
		}

		[Test]
		public void AgentWorks()
		{
			CreateRequestForApartment();
			var adress = browser.Url;
			var _params =
				adress.Split(new char[] {'?'}).Last().Split(new char[] {'&'}).Select(a => a.Split(new char[] {'='}).Last());
			var house = House.Find(UInt32.Parse(_params.First()));
			var apartment = UInt32.Parse(_params.Last());
			browser.Button("register_button").Click();

			var requests = new List<Request>();
			Partner partner = null;
			//using (new SessionScope())
			{
				requests = Request.Queryable.Where(
					r =>
					r.Street == house.Street && r.CaseHouse == house.Case && r.House == house.Number && r.Apartment == apartment).
					ToList();
				Assert.That(requests.Count, Is.GreaterThan(0), "Нет заявок после регистрации");
				partner = Partner.Queryable.FirstOrDefault(p => p.Login == Environment.UserName);
				partner.Categorie.Id = 3;
				partner.Update();
			}
			var clientCode = string.Empty;
			using (var browser2 = Open("UserInfo/RequestView.rails"))
			{
				browser2.Link("request_to_reg_" + requests.First().Id).Click();
				var sw = browser2.SelectList("SelectSwitches").Options.Select(o => UInt32.Parse(o.Value)).ToList();
				//using (new SessionScope())
				{
					var diniedPorts = ClientEndpoints.Queryable.Where(c => c.Switch.Id == sw[1]).ToList().Select(c => c.Port).ToList();
					browser2.SelectList("SelectSwitches").SelectByValue(sw[1].ToString());
					browser2.TextField("Port").AppendText((diniedPorts.Max(p => p.Value) + 1).ToString());
				}
				browser2.SelectList("BrigadForConnect").SelectByValue(Brigad.FindFirst().Id.ToString());
				browser2.Button("RegisterClientButton").Click();
				Assert.That(browser2.Text, Is.StringContaining("Информация по клиенту"), "Не осуществлена регистрация клиента");
				clientCode = browser2.Url.Split(new char[] {'?'}).Last().Split(new char[] {'='}).Last();
			}
			//using (new SessionScope())
			{
				var payments =
					PaymentsForAgent.Queryable.Where(
						p => p.Comment.Contains(clientCode) || p.Comment.Contains(requests.First().Id.ToString())).ToList();
				Assert.That(payments.Count, Is.GreaterThanOrEqualTo(3), "Неверное количество платежей, какого-то не хватает");
				var requests_for_partner = Request.Queryable.Where(r => r.Registrator == partner).ToList();
				var deleted = requests_for_partner.Count - 8;
				foreach (var requestse in requests_for_partner)
				{
					if (deleted < 0)
						break;
					requestse.DeleteAndFlush();
					deleted--;
				}
				CreateRequestForApartment();
				browser.Button("register_button").Click();
				scope.Dispose();
				scope = new SessionScope();
				var bonuses = Request.Queryable.Where(r => r.Registrator == partner).ToList().Sum(r => r.VirtualBonus);
				Assert.That(bonuses, Is.EqualTo(0m), "Неверное количество бонусов, их не должно быть");
				while (Request.Queryable.Where(r => r.Registrator == partner).Count() < 10)
				{
					CreateRequestForApartment();
					browser.Button("register_button").Click();
					scope.Dispose();
					scope = new SessionScope();
				}

				var for_bonuses = Request.Queryable.Where(r => r.Registrator == partner).ToList();

				Thread.Sleep(2000);
				Assert.That(for_bonuses.Sum(f => f.VirtualBonus), Is.GreaterThanOrEqualTo(500m));
			}
		}

		[Test]
		public void BonusTest()
		{
			var partner = Partner.Queryable.FirstOrDefault(p => p.Login == Environment.UserName);
			while (Request.Queryable.Where(r => r.Registrator == partner).Count() < 20)
			{
				CreateRequestForApartment();
				browser.Button("register_button").Click();
				scope.Dispose();
				scope = new SessionScope();
			}
			var for_bonuses = Request.Queryable.Where(r => r.Registrator == partner).ToList();
			Thread.Sleep(2000);
			Assert.That(for_bonuses.Sum(f => f.VirtualBonus), Is.GreaterThanOrEqualTo(2000m));
		}
	}
}
