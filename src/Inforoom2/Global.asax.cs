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
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			log4net.Config.XmlConfigurator.Configure();
			InitializeSessionFactory();
		}

		protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
		{
			if (FormsAuthentication.CookiesSupported) {
				var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
				if (cookie != null) {
					var ticket = FormsAuthentication.Decrypt(cookie.Value);
					var clientId = 0;
					if (ticket != null && !string.IsNullOrEmpty(ticket.UserData)) {
						var impersonatedClientId = ticket.UserData;
						int.TryParse(impersonatedClientId, out clientId);
					}
					var userName = ticket.Name;

					if (clientId != 0) {
						userName = clientId.ToString();
					}
					var identity = new GenericIdentity(userName, "Forms");
					HttpContext.Current.User = new CustomPrincipal(identity, new List<Permission>(), new List<Role>());
				}
			}
		}

		public static void InitializeSessionFactory()
		{
			Configuration configuration = new Configuration();
			configuration.SetNamingStrategy(new TableNamingStrategy());
			var nhibernateConnectionString = ConfigurationManager.AppSettings["nhibernateConnectionString"];
			configuration.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider")
				.SetProperty("connection.driver_class", "NHibernate.Driver.MySqlDataDriver")
				.SetProperty("connection.connection_string", nhibernateConnectionString)
				.SetProperty("dialect", "NHibernate.Dialect.MySQL5Dialect")
				.SetProperty("current_session_context_class", "web");

			/*	var listener = new SyncObject();			configuration.EventListeners.PostUpdateEventListeners =
				new IPostUpdateEventListener[] { listener };
			configuration.EventListeners.PostInsertEventListeners =
				new IPostInsertEventListener[] { listener };*/

			/*	var configurationPath = HttpContext.Current.Server.MapPath(@"~\Nhibernate\hibernate.cfg.xml");
			configuration.Configure(configurationPath);*/
			configuration.AddInputStream(
				NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(Assembly.GetExecutingAssembly()));

			//TODO Раскоментировать для создания таблиц с помощью Nhibernate
			/*	var schema = new NHibernate.Tool.hbm2ddl.SchemaExport(configuration);
			schema.Create(false, true);*/
			SessionFactory = configuration.BuildSessionFactory();
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			Exception exception = Server.GetLastError();
			Response.Clear();
			HttpException httpException = exception as HttpException;
			RouteData routeData = new RouteData();
			routeData.Values.Add("controller", "StaticContent");
			if (httpException == null) {
				routeData.Values.Add("action", "Error");
			}
			else //It's an Http Exception, Let's handle it.
			{
				switch (httpException.GetHttpCode()) {
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
	}
}