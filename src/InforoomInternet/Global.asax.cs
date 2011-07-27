using System;
using System.IO;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Container;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.JSGeneration;
using Castle.MonoRail.Framework.JSGeneration.jQuery;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Views.Brail;
using InforoomInternet.Initializers;
using InternetInterface.Helpers;
using log4net;
using log4net.Config;

namespace InforoomInternet
{
	public class Global : HttpApplication, IMonoRailConfigurationEvents, IMonoRailContainerEvents
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		void Application_Start(object sender, EventArgs e)
		{
			try {
				XmlConfigurator.Configure();
				GlobalContext.Properties["Version"] = Assembly.GetExecutingAssembly().GetName().Version;
                ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
				new ActiveRecord().Initialize(ActiveRecordSectionHandler.Instance);

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

		void Application_Error(object sender, EventArgs e) {
			var exception = Server.GetLastError();

			if (exception is ControllerNotFoundException
				&& !Request.UrlReferrer.AbsolutePath.Contains("ivrn.net"))
			{
				return;
			}

			var builder = new StringBuilder();
			builder.AppendLine("----UrlReferer-------");
			builder.AppendLine(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : String.Empty);
			builder.AppendLine("----Url-------");
			builder.AppendLine(Request.Url.ToString());
			builder.AppendLine("--------------");
			builder.AppendLine("----Params----");
			foreach (string name in Request.QueryString)
				builder.AppendLine(String.Format("{0}: {1}", name, Request.QueryString[name]));
			builder.AppendLine("--------------");

			builder.AppendLine("----Error-----");
			do {
				builder.AppendLine("Message:");
				builder.AppendLine(exception.Message);
				builder.AppendLine("Stack Trace:");
				builder.AppendLine(exception.StackTrace);
				builder.AppendLine("--------------");
				exception = exception.InnerException;
			} while (exception != null);
			builder.AppendLine("--------------");

			builder.AppendLine("----Session---");
			try {
				foreach (string key in Session.Keys) {
					if (Session[key] == null)
						builder.AppendLine(String.Format("{0} - null", key));
					else
						builder.AppendLine(String.Format("{0} - {1}", key, Session[key]));
				}
			}
			catch (Exception ex) { }
			builder.AppendLine("--------------");

			_log.Error(builder.ToString());
/*
#if !DEBUG
			Response.Redirect("~/Rescue/Error.aspx");
#endif
*/
		}

		void Session_End(object sender, EventArgs e) { }

		void Application_End(object sender, EventArgs e) { }

		public void Configure(IMonoRailConfiguration configuration) {
			configuration.ControllersConfig.AddAssembly("InforoomInternet");
			configuration.ViewComponentsConfig.Assemblies = new[] {"InforoomInternet"};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.UrlConfig.UseExtensions = false;
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			/*configuration.JSGeneratorConfiguration.AddLibrary("jquery-1.2.1", typeof(JQueryGenerator))
	.AddExtension(typeof(CommonJSExtension))
	.AddExtension(typeof(InterfaceLibEffects))
	.ElementGenerator
		.AddExtension(typeof(SomeElementLevelExtension))
		.Done
	.BrowserValidatorIs(typeof(VinterValidatorProvider))
	.SetAsDefault();*/
			/*	
			configuration.SmtpConfig.Host = "mail.adc.analit.net";
			configuration.ExtensionEntries.Add(new ExtensionEntry(typeof(ExceptionChainingExtension),
			new MutableConfiguration("mailTo")));
			*/
		}

		public void Created(IMonoRailContainer container)
		{ }

		public void Initialized(IMonoRailContainer container)
		{
			((DefaultViewComponentFactory)container.GetService<IViewComponentFactory>()).Inspect(Assembly.Load("InforoomInternet"));
		}
	}
}