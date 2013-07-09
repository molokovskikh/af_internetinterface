using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Helpers;
using log4net;

namespace InternetInterface
{
	public class AppConfig
	{
		public string PrinterPath { get; set; }
	}

	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		public static AppConfig Config = new AppConfig();

		public Global()
			: base(Assembly.Load("InternetInterface"))
		{
			FixMonorailConponentBug = false;
			Logger.ErrorSubject = "[Internet] Ошибка в Интернет интерфейсе";
			Logger.SmtpHost = "box.analit.net";
		}

		private void Application_Start(object sender, EventArgs e)
		{
			TypeDescriptor.AddProvider(new IPAddressTypeDescriptorProvider(), typeof(IPAddress));
			ConfigReader.LoadSettings(Config);
			Initialize();

			RoutingModuleEx.Engine.Add(new PatternRoute("/")
				.DefaultForController().Is("Login")
				.DefaultForAction().Is("LoginPartner"));
			RoutingModuleEx.Engine.Add(new PatternRoute("/<controller>/<action>"));
			RoutingModuleEx.Engine.Add(new PatternRoute("/<controller>/<id>/<action>")
				.Restrict("id").ValidInteger);
		}

		public void Configure(IMonoRailConfiguration configuration)
		{
			configuration.ControllersConfig.AddAssembly("InternetInterface");
			configuration.ViewComponentsConfig.Assemblies = new[] {
				"InternetInterface",
				"Common.Web.Ui"
			};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(WebFormsViewEngine), false));
			configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			base.Configure(configuration);
		}
	}
}