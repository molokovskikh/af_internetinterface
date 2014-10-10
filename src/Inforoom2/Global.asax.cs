using System;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;

namespace Inforoom2
{
	public class MvcApplication : System.Web.HttpApplication
	{
		public static ISessionFactory SessionFactory { get; set; }

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			RouteConfig.RegisterRoutes(RouteTable.Routes);
		}

		protected void Application_EndRequest(object sender, EventArgs e)
		{
			{
				if (SessionFactory != null) {
					SessionFactory.Close();
				}
			}
		}

		public static ISession OpenSession()
		{
			if (SessionFactory == null) //not threadsafe
			{
				//SessionFactories are expensive, create only once
				Configuration configuration = new Configuration();
				var configurationPath = HttpContext.Current.Server.MapPath(@"~\Nhibernate\hibernate.cfg.xml");
				configuration.Configure(configurationPath);

				configuration.AddInputStream(
					NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(Assembly.GetExecutingAssembly()));
				/*	NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(
						typeof (Order)));*/
				SessionFactory = configuration.BuildSessionFactory();
			}
			return SessionFactory.OpenSession();
		}

		protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
		{
			if (FormsAuthentication.CookiesSupported == true) {
				if (Request.Cookies[FormsAuthentication.FormsCookieName] != null) {
					try {
						//let us take out the username now                
						string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
						string roles = string.Empty;
						var DBSession = MvcApplication.OpenSession();
						User user = DBSession.Query<User>().SingleOrDefault(u => u.Username == username);
						roles = user.Roles;
						//Let us set the Pricipal with our user specific details
						HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(
							new System.Security.Principal.GenericIdentity(username, "Forms"), roles.Split(';'));
					}

					catch
						(Exception) {
						//somehting went wrong
					}
				}
			}
		}
	}
}