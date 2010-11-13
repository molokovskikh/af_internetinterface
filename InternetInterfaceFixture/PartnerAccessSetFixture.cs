using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using log4net.Config;
using NUnit.Framework;

namespace InternetInterfaceFixture
{

	[TestFixture]
	public class PartnerAccessSetFixture : PartnerAccessSet
	{

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

		[Test]
		public void AccessSearchTest()
		{
			//InithializeContent.partner = Castle.MonoRail
			Console.WriteLine(AccesPartner(AccessCategoriesType.GetClientInfo));
		}
	}
}
