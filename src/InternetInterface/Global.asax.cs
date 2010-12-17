using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Castle.ActiveRecord;
using System.Reflection;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;
using log4net;
using log4net.Config;

namespace InternetInterface
{
	public class Global : HttpApplication, IMonoRailConfigurationEvents
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

		void Application_Start(object sender, EventArgs e)
		{
			XmlConfigurator.Configure();
			// Code that runs on application startup
			ActiveRecordStarter.Initialize(
					Assembly.Load("InternetInterface")
	,
	ActiveRecordSectionHandler.Instance);

			RoutingModuleEx.Engine.Add(new PatternRoute("/")
	.DefaultForController().Is("Login")
	.DefaultForAction().Is("LoginPartner"));
		}

		void Application_End(object sender, EventArgs e)
		{
			//  Code that runs on application shutdown

		}

		void Application_Error(object sender, EventArgs e)
		{
			var exception = Server.GetLastError();

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
			do
			{
				builder.AppendLine("Message:");
				builder.AppendLine(exception.Message);
				builder.AppendLine("Stack Trace:");
				builder.AppendLine(exception.StackTrace);
				builder.AppendLine("--------------");
				exception = exception.InnerException;
			} while (exception != null);
			builder.AppendLine("--------------");

			builder.AppendLine("----Session---");
			try
			{
				foreach (string key in Session.Keys)
				{
					if (Session[key] == null)
						builder.AppendLine(String.Format("{0} - null", key));
					else
						builder.AppendLine(String.Format("{0} - {1}", key, Session[key]));
				}
			}
			catch (Exception ex)
			{ }
			builder.AppendLine("--------------");

			_log.Error(builder.ToString());

		}

		void Session_Start(object sender, EventArgs e)
		{
			// Code that runs when a new session is started

		}

		public void Configure(IMonoRailConfiguration configuration)
		{
			configuration.ControllersConfig.AddAssembly("InternetInterface");
			//configuration.ControllersConfig.AddAssembly("Common.Web.Ui");
			configuration.ViewComponentsConfig.Assemblies = new[] {
				"InternetInterface"//,
				//"Common.Web.Ui"
			};
			configuration.ViewEngineConfig.ViewPathRoot = "Views";
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(BooViewEngine), false));
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof(WebFormsViewEngine), false));
			//configuration.ViewEngineConfig.AssemblySources.Add(new AssemblySourceInfo("Common.Web.Ui", "Common.Web.Ui.Views"));
			configuration.ViewEngineConfig.VirtualPathRoot = configuration.ViewEngineConfig.ViewPathRoot;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration.ViewEngineConfig.ViewPathRoot);

			MonoRail.Debugger.Toolbar.Toolbar.Init(configuration);
			/*			configuration.SmtpConfig.Host = "mail.adc.analit.net";
			configuration.ExtensionEntries.Add(new ExtensionEntry(typeof(ExceptionChainingExtension),
				new MutableConfiguration("mailTo")));*/
		}

		void Session_End(object sender, EventArgs e)
		{
			// Code that runs when a session ends. 
			// Note: The Session_End event is raised only when the sessionstate mode
			// is set to InProc in the Web.config file. If session mode is set to StateServer 
			// or SQLServer, the event is not raised.

		}

	}
}
