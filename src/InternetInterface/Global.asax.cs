using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Castle.ActiveRecord;
using System.Reflection;
using Castle.ActiveRecord.Framework.Config;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Views.Aspx;
using Castle.MonoRail.Views.Brail;

namespace InternetInterface
{
	public class Global : HttpApplication, IMonoRailConfigurationEvents
	{

		void Application_Start(object sender, EventArgs e)
		{
			// Code that runs on application startup
			ActiveRecordStarter.Initialize(
					Assembly.Load("InternetInterface")
	,
	ActiveRecordSectionHandler.Instance);

		}

		void Application_End(object sender, EventArgs e)
		{
			//  Code that runs on application shutdown

		}

		void Application_Error(object sender, EventArgs e)
		{
			// Code that runs when an unhandled error occurs

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
