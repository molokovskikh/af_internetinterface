using System;
using System.Linq;
using System.Collections.Generic;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class MapController : BaseController
	{
		public void SiteMap()
		{
			RedirectToUrl(InternetInterface.Helpers.GlobalNames.AdminPanelNew);
		//	PropertyBag["Bookmarks"] = DbSession.Query<Bookmark>().Where(b => b.Date.Date == DateTime.Now.Date && !b.Deleted).ToList();
		}

		public void ShowBookmarks([DataBind("period")] DatePeriod period)
		{
			if (period.Begin == DateTime.MinValue)
				period = new DatePeriod(DateTime.Now.AddDays(-7), DateTime.Now);
			PropertyBag["period"] = period;
			PropertyBag["Bookmarks"] = DbSession.Query<Bookmark>().Where(b => b.Date >= period.Begin && b.Date <= period.End).ToList();
		}

		public void Bookmark(uint id)
		{
			var bookmark = DbSession.Get<Bookmark>(id);
			if (bookmark == null) {
				bookmark = new Bookmark { Date = DateTime.Now };
			}
			PropertyBag["bookmark"] = bookmark;
			if (IsPost) {
				BindObjectInstance(bookmark, ParamStore.Form, "bookmark");
				if (IsValid(bookmark)) {
					DbSession.SaveOrUpdate(bookmark);
					RedirectToAction("ShowBookmarks");
					Notify("Сохранено");
				}
			}
		}

		public void DeleteBookmark(uint id)
		{
			var bookmark = DbSession.Get<Bookmark>(id);
			if (bookmark != null) {
				bookmark.Deleted = true;
				DbSession.SaveOrUpdate(bookmark);
				Notify("Удалено");
			}
			RedirectToReferrer();
		}
	}
}