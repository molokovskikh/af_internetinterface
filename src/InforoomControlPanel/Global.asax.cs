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
using NHibernate;
using NHibernate.Event;
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

		protected void Application_Error(object sender, EventArgs e)
		{
			try {
				Exception exception = Server.GetLastError();

#if DEBUG
				throw exception;
#endif
				var currentUrl = HttpContext.Current.Request.Url.ToString();
				var exText = string.Format("<a href='{0}'>{0}</a>", currentUrl.CutAfter(100)); 

				var plainTextBytes =
					Encoding.UTF8.GetBytes(string.Format("Произошла ошибка по адресу '{0}', '{1}' ", exText,
						ExceptionTopHandler.GetExceptionTextMessage(exception)));
				var text = Convert.ToBase64String(plainTextBytes);
				Response.Cookies.Add(new HttpCookie("ErrorMessage", text) {Path = "/"});
				if (Request.RequestContext != null) {
					Response.Redirect(new UrlHelper(Request.RequestContext).Action("Statistic", "Admin") ?? "/");
				}
			}
			catch (Exception) {
				//empty
			}
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
					using (var session = Inforoom2.MvcApplication.SessionFactory.OpenSession())
						employee = session.Query<Employee>().FirstOrDefault(k => k.Login == username);
					HttpContext.Current.User = employee != null
						? new CustomPrincipal(identity, employee.Permissions, employee.Roles)
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