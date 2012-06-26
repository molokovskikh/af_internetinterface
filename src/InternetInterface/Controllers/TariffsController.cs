using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof (AuthenticationFilter))]
	public class TariffsController : BaseController
	{
		public void ChangeRules()
		{
			var rules = DbSession.Query<TariffChangeRule>().ToList();
			var colums = rules.Select(l => l.FromTariff).Distinct().OrderBy(t => t.Name).ToList();
			var rows = rules.Select(l => l.ToTariff).Distinct().OrderBy(t => t.Name).ToList();
			PropertyBag["columns"] = colums;
			PropertyBag["rows"] = rows;
			PropertyBag["rules"] = rules;

			if (IsPost) {
				SetSmartBinder(AutoLoadBehavior.NullIfInvalidKey);
				rules = BindObject<List<TariffChangeRule>>("rules");
				if (IsValid(rules)) {
					foreach (var rule in rules)
						DbSession.Save(rule);
					Notify("Сохранено");
					RedirectToReferrer();
				}
			}
		}
	}
}