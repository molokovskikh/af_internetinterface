using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;
using Configuration = NHibernate.Cfg.Configuration;

namespace InforoomControlPanel
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
			InitializeSessionFactory();
		}

		protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
		{
			if (FormsAuthentication.CookiesSupported) {
				var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
				if (cookie != null) {
					var userData = FormsAuthentication.Decrypt(cookie.Value);
					var username = userData.Name;
					var identity = new GenericIdentity(username, "Forms");
					Employee employee;
					using (var session = Inforoom2.MvcApplication.SessionFactory.OpenSession()) {
						employee = session.Query<Employee>().FirstOrDefault(k => k.Login == username);
					}
					HttpContext.Current.User = employee != null ? new CustomPrincipal(identity, employee.Permissions, employee.Roles)
						: new CustomPrincipal(identity, new List<Permission>(), new List<Role>());
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
				NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(Assembly.GetAssembly(typeof(Client))));

			//TODO Раскоментировать для создания таблиц с помощью Nhibernate
			/*	var schema = new NHibernate.Tool.hbm2ddl.SchemaExport(configuration);
			schema.Create(false, true);*/
			SessionFactory = configuration.BuildSessionFactory();
		}

	}
}