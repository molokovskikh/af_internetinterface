using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class LinqEx
	{
		[LinqExtensionMethod]
		public static int Mod(int? n, int m)
		{
			throw new NotImplementedException();
		}
	}

	public enum HouseNumberType
	{
		[Description("Нечетный")] Odd,
		[Description("Четный")] Even
	}

	public class RequestFilter : PaginableSortable
	{
		public RequestFilter()
		{
			PageSize = 20;
		}

		public string query { get; set; }
		public DateTime? beginDate { get; set; }
		public DateTime? endDate { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }
		public uint? labelId { get; set; }
		public bool Archive { get; set; }

		[Description("Номера домов больше")]
		public int? HouseNumberBegin { get; set; }

		[Description("Номера домов меньше")]
		public int? HouseNumberEnd { get; set; }

		[Description("Четность номера дома")]
		public HouseNumberType? HouseNumberType { get; set; }

		public List<Request> Find(ISession session)
		{
			beginDate = beginDate ?? DateTime.Today.FirstDayOfMonth();
			endDate = endDate ?? DateTime.Today;

			Expression<Func<Request, bool>> predicate;
			if (labelId != 0)
				if (!string.IsNullOrEmpty(query))
					predicate = i => (i.Street.Contains(query)
						|| i.ApplicantPhoneNumber.Contains(query)
						|| i.ApplicantName.Contains(query))
						&& i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Label.Id == labelId && i.Archive == Archive;
				else {
					predicate = i => i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Label.Id == labelId && i.Archive == Archive;
				}
			else {
				if (!string.IsNullOrEmpty(query))
					predicate =
						i =>
							(i.Street.Contains(query) || i.ApplicantPhoneNumber.Contains(query) || i.ApplicantName.Contains(query)) && i.ActionDate.Date >= beginDate.Value.Date &&
								i.ActionDate.Date <= endDate.Value.Date &&
								i.Archive == Archive;
				else {
					predicate =
						i => i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Archive == Archive;
				}
			}

			var outId = 0u;
			if (UInt32.TryParse(query, out outId))
				predicate = i => i.Id == outId;

			var dbQuery = session.Query<Request>().Where(predicate);
			if (HouseNumberBegin != null) {
				dbQuery = dbQuery.Where(r => r.House >= HouseNumberBegin);
			}

			if (HouseNumberEnd != null) {
				dbQuery = dbQuery.Where(r => r.House <= HouseNumberEnd);
			}
			if (HouseNumberType != null) {
				var result = HouseNumberType.Value == Queries.HouseNumberType.Even ? 0 : 1;
				dbQuery = dbQuery.Where(r => LinqEx.Mod(r.House, 2) == result);
			}

			RowsCount = dbQuery.Count();
			if (RowsCount > 0) {
				var getCount = RowsCount - PageSize * CurrentPage < PageSize ? RowsCount - PageSize * CurrentPage : PageSize;
				var result = dbQuery.ToList();
				if (!string.IsNullOrEmpty(SortBy))
					result.Sort(new PropertyComparer<Request>(IsDesc() ? Common.Tools.SortDirection.Desc : Common.Tools.SortDirection.Asc, SortBy));
				return result.Skip(PageSize * CurrentPage)
					.Take(getCount)
					.ToList();
			}
			return new List<Request>();
		}
	}
}