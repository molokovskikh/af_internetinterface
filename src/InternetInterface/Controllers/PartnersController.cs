using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.SymbolStore;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Util;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PartnersController : BaseController
	{
		public PartnersController()
		{
			SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);
		}

		public void Index(uint roleId)
		{
			var role = DbSession.Load<UserRole>(roleId);
			PropertyBag["role"] = role;
			PropertyBag["partners"] = DbSession.Query<Partner>().Where(p => p.Role == role)
				.OrderBy(p => p.Name)
				.ToList();
		}

		public void New(uint roleId)
		{
			Partner partner;
			if (roleId == 0) {
				partner = new Partner();
				partner.RegDate = DateTime.Now;
			}
			else {
				var role = DbSession.Load<UserRole>(roleId);
				partner = new Partner(role);
			}
			PropertyBag["Partner"] = partner;
			if (IsPost && Form.Keys.Cast<string>().Any(k => k.StartsWith("Partner."))) {
				BindObjectInstance(partner, ParamStore.Form, "Partner");
				if (IsValid(partner)) {
#if !DEBUG
					if (ActiveDirectoryHelper.FindDirectoryEntry(partner.Login) == null) {
						var password = CryptoPass.GeneratePassword();
						ActiveDirectoryHelper.CreateUserInAD(partner.Login, password);
						Flash["PartnerPass"] = password;
					}
#endif
					DbSession.Save(partner);
					RedirectToAction("Report", new { partner.Id });
				}
			}
		}

		public void Edit(uint id)
		{
			var partner = DbSession.Load<Partner>(id);
			PropertyBag["Partner"] = partner;
			if (IsPost) {
				BindObjectInstance(partner, ParamStore.Form, "Partner");
				if (IsValid(partner)) {
					var agent = DbSession.Query<Agent>().FirstOrDefault(a => a.Partner == partner);
					if (agent != null) {
						agent.Name = partner.Name;
						DbSession.Save(agent);
					}

					Notify("Сохранено");
					RedirectToReferrer();
				}
			}
		}

		public void Report(uint id)
		{
			PropertyBag["Partner"] = DbSession.Load<Partner>(id);
		}
	}
}