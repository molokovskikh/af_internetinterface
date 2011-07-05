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
		/// <param name="exec"></param>
		/// <param name="context"></param>
		/// <param name="controller"></param>
		/// <param name="controllerContext"></param>
		/// <returns></returns>
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
#if DEBUG
			context.Session["Login"] = Environment.UserName;
#endif
			if (context.Session["Login"] == null)
			{
				context.Session["Login"] = context.CurrentUser.Identity.Name;
			}
			if (Partner.FindAllByProperty("Login", context.Session["Login"]).Length == 0)
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginPartner.brail");
				return false;
			}
			else
			{
				//InithializeContent.partner = Partner.GetPartnerForLogin(context.Session["login"].ToString());
				controllerContext.PropertyBag["PartnerAccessSet"] = new CategorieAccessSet();
				controllerContext.PropertyBag["MapPartner"] = InithializeContent.partner;
                if (CategorieAccessSet._accesedPartner == null)
			    CategorieAccessSet._accesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof (CategorieAccessSet))
			                                                                        .CreateAlias("AccessCat", "AC",
			                                                                                     JoinType.InnerJoin)
			                                                                        .Add(Restrictions.Eq("Categorie",
			                                                                                             InithializeContent.
			                                                                                                 partner.Categorie)))
			        .Select(
			            c => c.AccessCat.ReduceName).ToList();
				if (AccessRules.GetAccessName(controllerContext.Action).Count(CategorieAccessSet.AccesPartner) == 0)
				{
					context.Response.RedirectToUrl(@"..\\Errors\AccessDin.aspx");
					return false;
				}
				return true;
			}
			
		}
	}
}