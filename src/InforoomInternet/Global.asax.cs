using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
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
	public class Global : WebApplication, IMonoRailConfigurationEvents
	{
		public class MixedRouteHandler : IRouteHandler
		{
			public IHttpHandler GetHttpHandler(RequestContext requestContext)
			{
				IHttpHandler hander = null;
				try {
					hander = new MonoRailHttpHandlerFactory().GetHandler(HttpContext.Current, null, null, null);
				}
				catch (UrlTokenizerException) {
				}

				if (hander is MonoRailHttpHandlerFactory.NotFoundHandler)
					hander = null;

				if (hander == null)
					hander = new MvcHandler(requestContext);
				return hander;
			}

			public static void ConfigRoute()
			{
				var routes = RouteTable.Routes;
				routes.Add(new Route("{resource}.axd/{*pathInfo}", new StopRoutingHandler()));

				var route = new Route("{controller}/{action}/{id}", new MixedRouteHandler()) {
					Defaults = new RouteValueDictionary {
						{ "controller", "Home" },
						{ "action", "Index" },
						{ "id", new object() }
					},
					DataTokens = new RouteValueDictionary()
				};


				routes.Add(route);
			}
		}
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		public Global()
			: base(Assembly.Load("InforoomInternet"))
		{
			FixMonorailConponentBug = false;
			Logger = new HttpSessionLog(typeof(Global));
			LibAssemblies.Add(Assembly.Load("Common.Web.Ui"));
			Logger.ErrorSubject = "Ошибка в IVRN";
			Logger.ExcludeExceptionTypes.Add(typeof(ControllerNotFoundException));
			Logger.ExcludeExceptionTypes.Add(typeof(FileNotFoundException));
		}

		private void Application_Start(object sender, EventArgs e)
		{
			try {
				ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;

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
				_log.Fatal("Ошибка при запуске страницы.", ex);
			}
		}

		private void Session_Start(object sender, EventArgs e)
		{
		}

		private void Application_BeginRequest(object sender, EventArgs e)
		{
		}

		private void Application_AuthenticateRequest(object sender, EventArgs e)
		{
		}

		private void Session_End(object sender, EventArgs e)
		{
		}

		public void Configure(IMonoRailConfiguration configuration)
		{
			configuration.ControllersConfig.AddAssembly("InforoomInternet");
			configuration.ViewComponentsConfig.Assemblies = new[] { "InforoomInternet", "Common.Web.Ui" };
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.UrlConfig.UseExtensions = false;
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			base.Configure(configuration);
		}
	}
}