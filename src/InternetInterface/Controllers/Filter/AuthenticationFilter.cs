using System;
using System.Collections.Generic;
using System.Linq;
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
			var username = context.Session["Login"];
			if (username == null || Partner.FindAllByProperty("Login", username).Length == 0) {
				context.Response.RedirectToUrl(@"~/Login/LoginPartner");
				return false;
			}

			var partner = Partner.GetPartnerForLogin(context.Session["login"].ToString());
			partner.AccesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
				.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
				.Add(Restrictions.Eq("Categorie", partner.Role)))
				.Select(c => c.AccessCat.ReduceName).ToList();
			context.Items.Add("Administrator", partner);
			controllerContext.PropertyBag["PartnerAccessSet"] = new CategorieAccessSet();
			controllerContext.PropertyBag["MapPartner"] = partner;
			if (AccessRules.GetAccessName(controllerContext.Action).Count(CategorieAccessSet.AccesPartner) == 0
				&& !partner.HavePermissionTo(controllerContext.Name, controllerContext.Action)) {
				context.Response.RedirectToUrl("~/Errors/AccessDin.aspx");
				return false;
			}
			return true;
		}
	}
}