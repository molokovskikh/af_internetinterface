using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
using InternetInterface.Models;
using InternetInterface.Models.Access;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers.Filter
{
	public class AuthenticationFilter : IFilter
	{
		/// <summary>
		/// Фильтр разграничения доступа к методам контроллеров
		/// </summary>
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var username = GetLoginFromCookie(context);
			if (username == null || Partner.FindAllByProperty("Login", username).Length == 0) {
				context.Response.RedirectToUrl(@"~/Login/LoginPartner", new { redirect = context.Request.Url });
				return false;
			}

			var partner = Partner.GetPartnerForLogin(username);
			partner.AccesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
				.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
				.Add(Restrictions.Eq("Categorie", partner.Role)))
				.Select(c => c.AccessCat.ReduceName).ToList();
			context.Items.Add("Administrator", partner);
			controllerContext.PropertyBag["MapPartner"] = partner;
			if (AccessRules.GetAccessName(controllerContext.Action).Count(partner.AccesPartner) == 0
				&& !partner.HavePermissionTo(controllerContext.Name, controllerContext.Action)) {
				context.Response.RedirectToUrl("~/Errors/AccessDin.aspx");
				return false;
			}
			return true;
		}

		public static void SetLoginCookie(IEngineContext context, object login)
		{
			var ticket = new FormsAuthenticationTicket(
				1,
				login.ToString(),
				DateTime.Now,
				DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes),
				false,
				"",
				FormsAuthentication.FormsCookiePath);
			var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
			context.Response.CreateCookie(cookie);
		}

		public static string GetLoginFromCookie(IEngineContext context)
		{
			var cookie = context.Request.ReadCookie(FormsAuthentication.FormsCookieName);
			if (cookie != null) {
				var userData = FormsAuthentication.Decrypt(cookie);
				if (userData != null)
					return userData.Name;
			}
			return null;
		}
	}
}