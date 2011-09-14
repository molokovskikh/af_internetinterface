﻿using System;
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
	class HouseMapFixture : WatinFixture 
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
					Console.WriteLine(browser.Text);
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
			//using (var browser = Open("HouseMap/FindHouse.rails"))
			{
				/*browser.Button("FindHouseButton").Click();
				Assert.Greater(browser.Table("find_result_table").TableRows.Count, 0);
				browser.Link(Find.ByClass("houses_links")).Click();
				//browser.Link("huise_link_0").Click();
				browser.Link(Find.ByClass("apartment_link")).Click();
				browser.Button("CreateRequest").Click();
				browser.TextField("request_ApplicantName").AppendText("testReq test test");
				browser.TextField("request_ApplicantPhoneNumber").AppendText("8-900-900-90-90");
				browser.TextField("request_Entrance").AppendText("2");
				browser.TextField("request_Floor").AppendText("2");*/
				CreateRequestForApartment();
				var adress = browser.Url;
				Console.WriteLine(adress);
				var _params = adress.Split(new char[] {'?'}).Last().Split(new char[] {'&'}).Select(a => a.Split(new char[] {'='}).Last());
				var house = House.Find(UInt32.Parse(_params.First()));
				var apartment = UInt32.Parse(_params.Last());
				foreach (var el in _params)
					Console.WriteLine(el);
				browser.Button("register_button").Click();
				var requests = new List<Requests>();
				Partner partner = null;
				//using (new SessionScope())
				{
					requests = Requests.Queryable.Where(
						r =>
						r.Street == house.Street && r.CaseHouse == house.Case && r.House == house.Number && r.Apartment == apartment).
						ToList();
					Assert.That(requests.Count, Is.GreaterThan(0));
					partner = Partner.Queryable.FirstOrDefault(p => p.Login == Environment.UserName);
					partner.Categorie.Id = 3;
					partner.Update();
					Console.WriteLine(partner.Categorie.ReductionName);
				}
				var clientCode = string.Empty;
				using (var browser2 = Open("UserInfo/RequestView.rails"))
				{
					browser2.Link("request_to_reg_" + requests.First().Id).Click();
					Console.WriteLine(browser2.Url);
					var sw = browser2.SelectList("SelectSwitches").Options.Select(o => UInt32.Parse(o.Value)).ToList();
					using (new SessionScope())
					{
						var diniedPorts = ClientEndpoints.Queryable.Where(c => c.Switch.Id == sw[1]).ToList().Select(c => c.Port).ToList();
						browser2.SelectList("SelectSwitches").SelectByValue(sw[1].ToString());
						browser2.TextField("Port").AppendText((diniedPorts.Max(p => p.Value) + 1).ToString());
					}
					browser2.SelectList("BrigadForConnect").SelectByValue(Brigad.FindFirst().Id.ToString());
					browser2.Button("RegisterClientButton").Click();
					Assert.That(browser2.Text, Is.StringContaining("Информация по клиенту"));
					clientCode = browser2.Url.Split(new char[] { '?' }).Last().Split(new char[] { '=' }).Last();
					Console.WriteLine(clientCode);
				}
				//using (new SessionScope())
				{
					var payments =
						PaymentsForAgent.Queryable.Where(
							p => p.Comment.Contains(clientCode) || p.Comment.Contains(requests.First().Id.ToString())).ToList();
					Console.WriteLine(payments.Count);
					Assert.That(payments.Count, Is.GreaterThanOrEqualTo(3));
					var interval = DateHelper.GetWeekInterval(DateTime.Now);
					var paymentsPartner = partner.GetPaymentsForInterval(interval);
					if (paymentsPartner.Count >= 20)
					{

					}

				}
			}
		}
	}
}
