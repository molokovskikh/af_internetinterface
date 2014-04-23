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
		public static Func<Partner> GetPartner = () => {
			var httpContext = HttpContext.Current;
			if (httpContext == null)
				throw new Exception("HttpContext не инициализирован");

			var admin = (Partner)httpContext.Items["Administrator"];
			if (admin == null) {
				admin = Partner.GetPartnerForLogin(httpContext.User.Identity.Name);
				if (admin != null)
					httpContext.Items["Administrator"] = admin;
			}

			return admin;
		};

		public static Partner Partner
		{
			get
			{
				var admin = GetPartner();
				if (admin == null)
					throw new NotAuthorizedException();

				return admin;
			}
		}

		public static Partner TryGetPartner()
		{
			if (HttpContext.Current == null)
				return null;
			return GetPartner();
		}
	}
}