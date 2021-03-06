﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Web;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ServicesFixture : IntegrationFixture
	{
		[Test]
		public void QueryTest()
		{
			var messages = ClientEndpoint.FindAll(DetachedCriteria.For(typeof(ClientEndpoint))
				.CreateAlias("Client", "c", JoinType.InnerJoin)
				.CreateAlias("c.Message", "m", JoinType.InnerJoin)
				.Add(Expression.Eq("Switch.Id", 136u))).Select(e => e.Client.Message).ToList();
		}

		[Test]
		public void FilterTest()
		{
			var workerRequest = new SimpleWorkerRequest("", "", "", "http://test", new StreamWriter(new MemoryStream()));
			var context = new HttpContext(workerRequest);
			context.User = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			context.Items["Administrator"] = InitializeContent.GetPartner();
			HttpContext.Current = context;

			session.CreateSQLQuery("delete from ServiceRequest").ExecuteUpdate();
			var client = new Client();
			client.Save();
			var request = new ServiceRequest {
				Client = client,
				Free = true,
				RegDate = DateTime.Now.AddMonths(-1),
				Registrator = null
			};
			session.Save(request);
			scope.Flush();
			var filter = new ServiceRequestFilter();
			Assert.That(filter.Find(session).Count, Is.EqualTo(0));
			filter.Period = new DatePeriod(DateTime.Now.AddMonths(-1), DateTime.Now);
			Assert.That(filter.Find(session).Count, Is.EqualTo(1));
		}

		[Test]
		public void Filter_request_by_region()
		{
			var client = ClientHelper.Client(session);
			client.PhysicalClient.HouseObj = new House("Тест", 1, session.Query<RegionHouse>().First(r => r.Name == "Воронеж"));
			var request = new ServiceRequest {
				Client = client,
				Description = "test",
				Contact = "473-2606000",
				Performer = Partner.GetServiceEngineers(session).First()
			};
			session.Save(client);
			session.Save(request);

			var filter = new ServiceRequestFilter();
			filter.Region = client.PhysicalClient.HouseObj.Region;
			var requests = filter.Find(session);
			Assert.Contains(request.Id, requests.Select(r => r.Id).ToArray());
		}
	}
}