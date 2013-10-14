using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Routing;
using Castle.ActiveRecord;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InforoomInternet.Models;
using InternetInterface.Helpers;
using log4net;
using log4net.Repository.Hierarchy;

namespace InforoomInternet
{
	public class Global : WebApplication
	{
		public Global()
			: base(Assembly.Load("InforoomInternet"))
		{
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
			LibAssemblies.Add(Assembly.Load("InforoomInternet"));
			Logger.ErrorSubject = "Ошибка в IVRN";
		}

		private void Application_Start(object sender, EventArgs e)
		{
			try {
				ActiveRecordStarter.EventListenerComponentRegistrationHook += AuditListener.RemoveAuditListener;

				Initialize();

				MixedRouteHandler.ConfigRoute();

				RoutingModuleEx.Engine.Add(new PatternRoute("/")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Новости"));

				RoutingModuleEx.Engine.Add(new PatternRoute("/Warning")
					.DefaultForController().Is("Main")
					.DefaultForAction().Is("Warning"));

				//Эта страница находится гуглом по запросу Воронеж ООО Инфорум
				RoutingModuleEx.Engine.Add(new PatternRoute("/Main/requisite")
					.DefaultForController().Is("Content")
					.DefaultForAction().Is("Реквизиты"));
			}
			catch (Exception ex) {
				Log.Fatal("Ошибка при запуске страницы.", ex);
			}
		}
	}
}