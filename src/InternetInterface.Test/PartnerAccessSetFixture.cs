using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using log4net.Config;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace InternetInterface.Test
{

	[TestFixture]
	public class PartnerAccessSetFixture : CategorieAccessSet
	{

		[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					//Assembly.Load("InternetInterface.Test"),
				}, ActiveRecordSectionHandler.Instance);
		}

		[Test]
		public void AccessSearchTest()
		{
			//InithializeContent.partner = Castle.MonoRail
			//Console.WriteLine(AccesPartner(AccessCategoriesType.GetClientInfo));
		}
	}
}
