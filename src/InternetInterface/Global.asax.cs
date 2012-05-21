using System;
using System.IO;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using log4net;

namespace InternetInterface
{
	public class AppConfig
	{
		public string PrinterPath { get; set; }
	}

	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		public static AppConfig Config = new AppConfig();

		public Global()
			: base(Assembly.Load("InternetInterface"))
		{
			Logger.ErrorSubject = "[Internet] Ошибка в Интернет интерфейсе";
			Logger.SmtpHost = "box.analit.net";
		}

		void Application_Start(object sender, EventArgs e)
		{
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
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			base.Configure(configuration);
		}
	}
}
