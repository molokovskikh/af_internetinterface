﻿using System;
using System.IO;
using Castle.ActiveRecord;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using InforoomInternet.Models;
using InternetInterface.Helpers;
using log4net;

namespace InforoomInternet
{
	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		public Global()
			: base(Assembly.Load("InforoomInternet"))
		{
			Logger = new HttpSessionLog(typeof(Global));
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
			Logger.ErrorSubject = "Ошибка в IVRN";
			Logger.SmtpHost = "box.analit.net";
			Logger.ExcludeExceptionTypes.Add(typeof(ControllerNotFoundException));
			Logger.ExcludeExceptionTypes.Add(typeof(FileNotFoundException));
		}

		void Application_Start(object sender, EventArgs e)
		{
			try {
				ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;

				Initialize();

				RoutingModuleEx.Engine.Add(new PatternRoute("/")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Новости"));

				RoutingModuleEx.Engine.Add(new PatternRoute("/Warning")
					.DefaultForController().Is("Main")
					.DefaultForAction().Is("Warning"));

				//Эта страница находится гуглом по запросу воронеж ООО Инфорум
				RoutingModuleEx.Engine.Add(new PatternRoute("/Main/requisite")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Реквизиты"));
			}
			catch (Exception ex)
			{
				_log.Fatal("Ошибка при запуске страницы.", ex);
			}
		}

		void Session_Start(object sender, EventArgs e) { }

		void Application_BeginRequest(object sender, EventArgs e) { }

		void Application_AuthenticateRequest(object sender, EventArgs e) { }

		void Session_End(object sender, EventArgs e) { }

		void Application_End(object sender, EventArgs e)
		{
			ClientData.StopClearing();
		}

		public void Configure(IMonoRailConfiguration configuration) {
			configuration.ControllersConfig.AddAssembly("InforoomInternet");
			configuration.ViewComponentsConfig.Assemblies = new[] {"InforoomInternet", "Common.Web.Ui"};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.UrlConfig.UseExtensions = false;
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);
		}
	}
}