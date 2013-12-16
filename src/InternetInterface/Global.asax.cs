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

	public class Global : WebApplication
	{
		public static AppConfig Config = new AppConfig();

		public Global()
			: base(Assembly.Load("InternetInterface"))
		{
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
			Logger.ErrorSubject = "[Internet] Ошибка в Интернет интерфейсе";
		}

		private void Application_Start(object sender, EventArgs e)
		{
			InstallBundle("jquery.calendar.support");
			InstallBundle("jquery.validate");

			TypeDescriptor.AddProvider(new IPAddressTypeDescriptorProvider(), typeof(IPAddress));
			ConfigReader.LoadSettings(Config);
			Initialize();

			RoutingModuleEx.Engine.Add(new PatternRoute("/")
				.DefaultForController().Is("Login")
				.DefaultForAction().Is("LoginPartner"));
			RoutingModuleEx.Engine.Add(new PatternRoute("/<controller>/<action>"));
			RoutingModuleEx.Engine.Add(new PatternRoute("/ServiceRequests/<id>/<action>")
				.DefaultForController().Is("ServiceRequest")
				.Restrict("id").ValidInteger);
			RoutingModuleEx.Engine.Add(new PatternRoute("/<controller>/<id>/<action>")
				.Restrict("id").ValidInteger);
		}
	}
}