using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using System.Configuration;
using InternetInterface.Controllers;
using InternetInterface.Models;
using log4net.Config;
using NUnit.Framework;
using WatiN.Core;


namespace InternetInterfaceFixtute.RegisterTest
{
	[TestFixture]
	public class SearchFixture : SearchController
	{

	/*	[]
		public void Setup()
		{

		}*/
		[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterfaceFixture"),
				}, ActiveRecordSectionHandler.Instance);
		}

		public static string BuildTestUrl(string urlPart)
		{
			return String.Format("http://localhost:{0}/{1}",
								 ConfigurationManager.AppSettings["webPort"],
								 urlPart);
		}

		protected IE Open(string uri)
		{
			return new IE(BuildTestUrl(uri));
		}

		[Test]
		public void SearchTets()
		{
			GetClients(new UserSearchProperties { SearchBy = SearchUserBy.Auto }, 4, 1, "p");
			//var browser = Open()
		}
	}
}