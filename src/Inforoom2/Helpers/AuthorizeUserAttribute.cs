using System.Web;
using System.Web.Mvc;
using Inforoom2.Controllers;
using Inforoom2.Models;

namespace Inforoom2.Helpers
{
	public class AuthorizeUserAttribute : AuthorizeAttribute
	{
		public string Permissions { get; set; }
		public BaseController Controller { get; set; }
		
		protected override bool AuthorizeCore(HttpContextBase httpContext)
		{
			var isAuthorized = base.AuthorizeCore(httpContext);
			if (!isAuthorized) {
				return false;
			}
			var user = (httpContext.User as CustomPrincipal);
			return user != null && (user.HasRoles(Roles) || user.HasPermissions(Permissions));
		}
	}
}