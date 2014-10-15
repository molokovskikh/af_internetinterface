using System;
using System.Collections.Generic;
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
using NHibernate.Cfg;
using NHibernate.Linq;

namespace Inforoom2
{
	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			RouteConfig.RegisterRoutes(RouteTable.Routes);
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
					using (var session = NHibernateActionFilter.sessionFactory.OpenSession()) {
						employee = session.Query<Employee>().FirstOrDefault(k => k.Username == username);
					}
					HttpContext.Current.User = employee != null ? new CustomPrincipal(identity, employee.Permissions, employee.Roles)
						: new CustomPrincipal(identity, new List<Permission>(), new List<Role>());
				}
			}
		}
	}
}