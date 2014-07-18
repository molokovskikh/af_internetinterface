using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Helper(typeof(PaginatorHelper))]
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class BrigadsController : BaseController
	{
		public BrigadsController()
		{
			SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);
		}

		public void Index()
		{
			PropertyBag["brigads"] = DbSession.Query<Brigad>().OrderBy(b => b.Name).ToList();
		}

		public void New()
		{
			var brigad = new Brigad();
			PropertyBag["brigad"] = brigad;
			if (IsPost) {
				BindObjectInstance(brigad, "brigad");
				if (IsValid(brigad)) {
					DbSession.Save(brigad);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void Edit(uint id)
		{
			var brigad = DbSession.Load<Brigad>(id);
			PropertyBag["brigad"] = brigad;
			if (IsPost) {
				BindObjectInstance(brigad, "brigad");
				if (IsValid(brigad)) {
					DbSession.Save(brigad);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void ReportOnWork([SmartBinder("filter")] BrigadFilter filter)
		{
			PropertyBag["filter"] = filter;
			PropertyBag["SClients"] = filter.Find(DbSession);
		}
	}
}