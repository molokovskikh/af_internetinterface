using System;
using System.Collections.Generic;
using System.IO;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.MonoRailExtentions;
using ExposedObject;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[SetUpFixture]
	internal class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			Functional.Setup.ConfigTest();

			Functional.Setup.PrepareTestData();
			InitializeContent.GetPartner = () => {
				var partner = Partner.FindFirst();
				partner.AccesedPartner = new List<string>();
				partner.AccesedPartner.Add("SSI");
				return partner;
			};
			BaseMailer.ViewEngineManager = GetViewManager();
		}

		public static IViewEngineManager GetViewManager()
		{
			var config = new MonoRailConfiguration();
			config.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			config.ViewEngineConfig.ViewPathRoot = Path.Combine(@"..\..\..\InternetInterface", "Views");

			var provider = new FakeServiceProvider();
			var loader = new FileAssemblyViewSourceLoader(config.ViewEngineConfig.ViewPathRoot);
			provider.Services.Add(typeof(IMonoRailConfiguration), config);
			provider.Services.Add(typeof(IViewSourceLoader), loader);

			var manager = new DefaultViewEngineManager();
			manager.Service(provider);
			var options = Exposed.From(manager).viewEnginesFastLookup[0].Options;
			var namespaces = options.NamespacesToImport;
			namespaces.Add("Boo.Lang.Builtins");
			return manager;
		}
	}
}
