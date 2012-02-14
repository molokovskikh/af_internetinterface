using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Web;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ServicesFixture : IntegrationFixture
	{
		[Test]
		public void FilterTest()
		{
			var workerRequest = new SimpleWorkerRequest("", "", "", "http://test", new StreamWriter(new MemoryStream()));
			var context = new HttpContext(workerRequest);
			context.User = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			context.Items[InitializeContent.AdministratorKey] = Partner.FindFirst();
			HttpContext.Current = context;

			ServiceRequest.DeleteAll();
			var client = new Client();
			client.Save();
			new ServiceRequest {
				Client = client,
				Free = true,
				RegDate = DateTime.Now.AddMonths(-1),
				Registrator = null
			}.Save();
			scope.Flush();
			var filter = new RequestFinderFilter();
			Assert.That(filter.Find().Count, Is.EqualTo(0));
			filter.Period = new DatePeriod(DateTime.Now.AddMonths(-1), DateTime.Now);
			Assert.That(filter.Find().Count, Is.EqualTo(1));
		}
	}
}
