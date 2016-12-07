using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using log4net;
using log4net.Config;
using NHibernate;
using NHibernate.Event;
using NHibernate.Linq;
using Configuration = NHibernate.Cfg.Configuration;

namespace InforoomControlPanel
{
	public class MvcApplication : System.Web.HttpApplication
	{
		static ILog Log = LogManager.GetLogger(typeof(MvcApplication));

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
			try { 
			XmlConfigurator.Configure();
			AreaRegistration.RegisterAllAreas();
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			InitializeSessionFactory();
				} catch(Exception e) {
					Log.Error("Ошибка при инициализации приложения", e);
					throw;
				}
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			try {
				ThreadContext.Properties["url"] = Request.Url;
				ThreadContext.Properties["urlReferrer"] = Request.UrlReferrer;
				ThreadContext.Properties["employee"] = HttpContext.Current.User?.Identity.Name;
			} catch {
			}
			var exception = Server.GetLastError();
#if DEBUG
			throw exception;
#endif
			Log.Error("[internet] Ошибка на сайте административной панели", exception);
			Response.Redirect(new UrlHelper(Request.RequestContext).Action("Error", "AdminOpen"));
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
			configuration.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] {new ModelCrudListener()};
			configuration.EventListeners.PostInsertEventListeners = new IPostInsertEventListener[] {new ModelCrudListener()};
			configuration.EventListeners.PreDeleteEventListeners = new IPreDeleteEventListener[] {new ModelCrudListener()};
			/*	var listener = new SyncObject();			configuration.EventListeners.PostUpdateEventListeners =
				new IPostUpdateEventListener[] { listener };
			configuration.EventListeners.PostInsertEventListeners =
				new IPostInsertEventListener[] { listener };*/

			/*	var configurationPath = HttpContext.Current.Server.MapPath(@"~\Nhibernate\hibernate.cfg.xml");
			configuration.Configure(configurationPath);*/
			configuration.AddInputStream(
				NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(Assembly.GetAssembly(typeof (Client))));

			//TODO Раскоментировать для создания таблиц с помощью Nhibernate
			/*	var schema = new NHibernate.Tool.hbm2ddl.SchemaExport(configuration);
			schema.Create(false, true);*/
			SessionFactory = configuration.BuildSessionFactory();
		}
	}
}