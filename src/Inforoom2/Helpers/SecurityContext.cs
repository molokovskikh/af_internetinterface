using System;
using System.Web;

namespace Inforoom2.Helpers
{
	public static class SecurityContext
	{
		public static Func<string> GetCurrentEmployeeName = () => {
			var httpContext = HttpContext.Current;
			if (httpContext == null)
				return "";

			if (httpContext.User == null || httpContext.User.Identity == null)
				return "";
			return httpContext.User.Identity.Name;
		};

		public static string CurrentEmployeeName
		{
			get { return GetCurrentEmployeeName(); }
		}
	}
}