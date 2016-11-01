using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Context;
using NHibernate.Event;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using Configuration = NHibernate.Cfg.Configuration;

namespace Inforoom2
{
	public class MvcApplication : System.Web.HttpApplication
	{
		private static ISessionFactory _sessionFactory;

		public static ISessionFactory SessionFactory
		{
			get
			{
				if (_sessionFactory != null) {
					return _sessionFactory;
				}
				else {
					InitializeSessionFactory();
					return _sessionFactory;
				}
			}
			private set { _sessionFactory = value; }
		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			RegisterRoutes(RouteTable.Routes);
			log4net.Config.XmlConfigurator.Configure();
			InitializeSessionFactory();
			SceHelper.StartRun();
		}

		protected void Application_End()
		{
			SceHelper.StopRun();
		}

		public override string GetVaryByCustomString(HttpContext context, string argList)
		{
			var result = string.Empty;
			var format = "{0}={1};";
			var args = argList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var arg in args) {
				if (arg == "User") {
					result += string.Format(format, "User", context.User.Identity.Name);
				}
				if (arg == "Cookies") {
					var cookie = context.Request.Cookies.Get("userCity");
					result += string.Format(format, "Cookies", (cookie != null ? cookie.Value : ""));
				}
			}
			return string.IsNullOrEmpty(result) ? base.GetVaryByCustomString(context, argList) : result;
		}

		protected
			void Application_PostAuthenticateRequest(Object sender, EventArgs e)
		{
		}

		public static
			void InitializeSessionFactory()
		{
			Configuration configuration = new Configuration();
			configuration.SetNamingStrategy
				(
					new TableNamingStrategy());
			var nhibernateConnectionString = ConfigurationManager.AppSettings["nhibernateConnectionString"];
			configuration.SetProperty
				("connection.provider", "NHibernate.Connection.DriverConnectionProvider")
				.
				SetProperty("connection.driver_class", "NHibernate.Driver.MySqlDataDriver")
				.
				SetProperty("connection.connection_string",
					nhibernateConnectionString
				)
				.
				SetProperty("dialect", "NHibernate.Dialect.MySQL5Dialect")
				.
				SetProperty("current_session_context_class", "web");
			configuration.EventListeners.PreUpdateEventListeners
				=
				new IPreUpdateEventListener[] {
					new ModelCrudListener()
				}
				;
			configuration.EventListeners.PostInsertEventListeners
				=
				new IPostInsertEventListener[] {
					new ModelCrudListener()
				}
				;
			configuration.EventListeners.PreDeleteEventListeners
				=
				new IPreDeleteEventListener[] {
					new ModelCrudListener()
				}
				;
			/*	var listener = new SyncObject();			configuration.EventListeners.PostUpdateEventListeners =
				new IPostUpdateEventListener[] { listener };
			configuration.EventListeners.PostInsertEventListeners =
				new IPostInsertEventListener[] { listener };*/

			/*	var configurationPath = HttpContext.Current.Server.MapPath(@"~\Nhibernate\hibernate.cfg.xml");
			configuration.Configure(configurationPath);*/
			configuration.AddInputStream
				(
					NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize
						(
							Assembly.GetExecutingAssembly
								()));

			//TODO Раскоментировать для создания таблиц с помощью Nhibernate
			/*	var schema = new NHibernate.Tool.hbm2ddl.SchemaExport(configuration);
			schema.Create(false, true);*/
			SessionFactory
				=
				configuration.BuildSessionFactory
					();
		}

		protected
			void Application_Error(object sender, EventArgs e)
		{
			bool showErrorPage = false;
			bool
				.
				TryParse(ConfigurationManager.AppSettings["ShowErrorPage"], out
					showErrorPage
				);
			if (!
				showErrorPage
				) {
				return;
			}
			Exception exception = Server.GetLastError();
			Response.Clear
				();
			HttpException httpException = exception as HttpException;
			RouteData routeData = new RouteData();
			routeData.Values.Add
				("controller", "StaticContent");

			if (
				httpException
				== null) {
				routeData.Values.Add("action", "Error");
			}
			else //It's an Http Exception, Let's handle it.
			{
				switch (
					httpException.GetHttpCode()) {
						case 404:
							// Page not found.
							routeData.Values.Add("action", "PageNotFound");
							break;
						case 500:
							// Server error.
							routeData.Values.Add("action", "HttpError500");
							break;

						// Here you can handle Views to other error codes.
						// I choose a General error template  
						default:
							routeData.Values.Add("action", "Error");
							break;
				}
			}

			// Pass exception details to the target error View.
			routeData.Values.Add("error", exception);

			// Clear the error on server.
			Server.ClearError();

			// Avoid IIS7 getting in the middle
			Response.TrySkipIisCustomErrors = true;

			// Call target Controller and pass the routeData.
			IController errorController = new StaticContentController();
			errorController.Execute(new RequestContext(
				new HttpContextWrapper(Context), routeData));
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.MapRoute("old index", "Main/Index", new { controller = "Home", action = "PermanentHomeRedirect" });
			routes.MapRoute("Default", "{controller}/{action}/{id}",
				new { controller = "Home", action = "Index", id = UrlParameter.Optional });
		}
	}
}