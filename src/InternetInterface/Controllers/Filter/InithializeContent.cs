using System;
using System.Linq;
using System.Web;
using InternetInterface.Models;

namespace InternetInterface.Controllers.Filter
{
	public class NotAuthorizedException : Exception
	{
	}

	public class InitializeContent
	{
		public const string AdministratorKey = "Administrator";

		public static Func<Partner> GetAdministrator = () => {
			var httpContext = HttpContext.Current;
			if (httpContext == null)
				throw new Exception("HttpContext не инициализирован");

			var admin = (Partner)httpContext.Items[AdministratorKey];
			if (admin == null) {
				admin = Partner.GetPartnerForLogin(httpContext.User.Identity.Name);
				if (admin != null)
					httpContext.Items[AdministratorKey] = admin;
			}

			return admin;
		};


		public static Partner Partner
		{
			get
			{
				var admin = GetAdministrator();
				if (admin == null)
					throw new NotAuthorizedException();

				return admin;
			}
		}
	}
}