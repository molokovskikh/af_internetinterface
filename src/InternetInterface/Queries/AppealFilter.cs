using System;
using System.Collections.Generic;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Queries
{
	public class AppealFilter : Sortable, SortableContributor, IPaginable
	{
		public string query { get; set; }
		public DateTime? startDate { get; set; }
		public DateTime? endDate { get; set; }
		public AppealTypeProperties appealType { get; set; }

		public int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 20; }
		}

		public int CurrentPage { get; set; }

		public string[] ToUrl()
		{
			return new[] {
				String.Format("filter.appealType.appealType={0}", appealType.appealType),
				String.Format("filter.startDate={0}", startDate),
				String.Format("filter.endDate={0}", endDate),
				String.Format("filter.query={0}", query)
			};
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public string GetUri()
		{
			return ToUrlQuery();
		}

		public List<Appeals> Find()
		{
			var thisD = DateTime.Now;
			if (startDate == null)
				startDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;
			if (appealType == null)
				appealType = new AppealTypeProperties();
			var totalRes =
				Appeals.Queryable.Where(a => a.Date.Date >= startDate.Value.Date && a.Date.Date <= endDate.Value.Date)
					.OrderByDescending(o => o.Date).ToList();
			if (appealType.appealType != AppealType.All)
				totalRes = totalRes.Where(a => a.AppealType == appealType.appealType).ToList();
			if (!string.IsNullOrEmpty(query))
				totalRes = totalRes.Where(t => t.Appeal.ToLower().Contains(query.ToLower())).ToList();
			_lastRowsCount = totalRes.Count();
			if (_lastRowsCount > 0) {
				var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
				return
					totalRes.GetRange(PageSize * CurrentPage, getCount);
			}
			return new List<Appeals>();
		}
	}
}