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
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class TariffsController : BaseController
	{
		public TariffsController()
		{
			SetSmartBinder(AutoLoadBehavior.NullIfInvalidKey);
		}

		public void Index()
		{
			PropertyBag["tariffs"] = DbSession.Query<Tariff>().OrderBy(t => t.Name).ToList();
		}

		public void Edit(uint id)
		{
			var tariff = DbSession.Load<Tariff>(id);
			PropertyBag["tariff"] = tariff;
			if (IsPost) {
				BindObjectInstance(tariff, "tariff");
				if (IsValid(tariff)) {
					DbSession.Save(tariff);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void ChangeRules()
		{
			var rules = DbSession.Query<TariffChangeRule>().ToList();
			var colums = rules.Select(l => l.FromTariff).Distinct().OrderBy(t => t.Name).ToList();
			var rows = rules.Select(l => l.ToTariff).Distinct().OrderBy(t => t.Name).ToList();
			PropertyBag["columns"] = colums;
			PropertyBag["rows"] = rows;
			PropertyBag["rules"] = rules;

			if (IsPost) {
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