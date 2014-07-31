using System;
using System.Collections.Generic;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using System.Web;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class AppealFilter : PaginableSortable
	{
		public AppealFilter()
		{
			AppealType = AppealType.All;
			PageSize = 20;
		}


		public string Query { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public AppealType AppealType { get; set; }

		public List<Appeals> Find(ISession session)
		{
			var thisD = DateTime.Now;
			if (StartDate == null)
				StartDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (EndDate == null)
				EndDate = DateTime.Now;

			var totalRes = session.Query<Appeals>()
				.Where(a => a.Date.Date >= StartDate.Value.Date && a.Date.Date <= EndDate.Value.Date)
				.OrderByDescending(o => o.Date)
				.ToList();
			if (AppealType != AppealType.All)
				totalRes = totalRes.Where(a => a.AppealType == AppealType).ToList();
			if (!string.IsNullOrEmpty(Query))
				totalRes = totalRes.Where(t => t.Appeal.ToLower().Contains(Query.ToLower())).ToList();
			RowsCount = totalRes.Count();
			if (RowsCount > 0) {
				var getCount = RowsCount - PageSize * CurrentPage < PageSize ? RowsCount - PageSize * CurrentPage : PageSize;
				return
					totalRes.GetRange(PageSize * CurrentPage, getCount);
			}
			return new List<Appeals>();
		}
	}
}