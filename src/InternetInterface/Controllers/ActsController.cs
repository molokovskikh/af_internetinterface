using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ActsController : BaseController
	{
		public void Index([SmartBinder] ActsFilter filter)
		{
			PropertyBag["filter"] = filter;
			PropertyBag["acts"] = filter.Find(DbSession);
			PropertyBag["printers"] = Printer.All();
		}

		public void Edit(uint id)
		{
			var act = DbSession.Load<Act>(id);
			PropertyBag["act"] = act;

			SaveIfNeeded(act);
		}

		private void SaveIfNeeded(Act act)
		{
			if (IsPost) {
				DoNotRecreateCollectionBinder.Prepare(this, "act.Parts");
				BindObjectInstance(act, "act");
				if (IsValid(act)) {
					act.CalculateSum();
					DbSession.SaveOrUpdate(act);
					Notify("Сохранено");
					RedirectToAction("Edit", new { id = act.Id });
				}
			}
		}

		public void Print(uint id)
		{
			LayoutName = "print";
			PropertyBag["act"] = DbSession.Load<Act>(id);
		}

		public void Process()
		{
			var binder = new ARDataBinder();
			binder.AutoLoad = AutoLoadBehavior.Always;
			SetBinder(binder);
			var acts = BindObject<Act[]>("acts");

			if (acts.Length == 0)
				RedirectToReferrer();

			if (Form["delete"] != null) {
				foreach (var act in acts)
					DbSession.Delete(act);

				Notify("Удалено");
			}

			/*if (Form["email"] != null) {
				foreach (var act in acts)
					this.Mailer().Act(act).Send();

				Notify("Отправлено");
			}*/

			if (Form["print"] != null) {
				var printer = Form["printer"];
				var arguments = String.Format("act \"{0}\" \"{1}\"", printer, acts.Implode(a => a.Id));
				Printer.Execute(arguments);

				Notify("Отправлено на печать");
			}
			RedirectToReferrer();
		}
	}
}