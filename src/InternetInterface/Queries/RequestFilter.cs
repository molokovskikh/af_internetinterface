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
		[Description("Введите Адрес / Телефон / ФИО / Номер заявки")]
		public string query { get; set; }
		[Description("Город")]
		public string City { get; set; }
		public DateTime? beginDate { get; set; }
		public DateTime? endDate { get; set; }
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
			//фильтр по датам и архиву
			beginDate = beginDate ?? DateTime.Today.FirstDayOfMonth();
			endDate = endDate ?? DateTime.Today;
			var dbQuery = session.Query<Request>().Where(r => r.Archive == Archive && (r.ActionDate.Date >= beginDate.Value.Date && r.ActionDate.Date <= endDate.Value.Date));

			var outId = 0u;
			//фильтр id или по улице, телефону и имени 
			if(UInt32.TryParse(query, out outId)) {
				dbQuery = dbQuery.Where(r => r.Id == outId);
			}
			else if(!string.IsNullOrEmpty(query)) {
				dbQuery = dbQuery.Where(r => r.Street.Contains(query) || r.ApplicantPhoneNumber.Contains(query) || r.ApplicantName.Contains(query));
			} 

			//фильтр по городу
			if (!string.IsNullOrEmpty(City)) {
				dbQuery = dbQuery.Where(r => r.City.Contains(City));
			}

			//фильтр по меткам
			if (labelId != 0) {
				dbQuery = dbQuery.Where(r => r.Label.Id == labelId);
			}

			//фильтр по дому1
			if (HouseNumberBegin != null) {
				dbQuery = dbQuery.Where(r => r.House >= HouseNumberBegin);
			}

			//фильтр по дому2
			if (HouseNumberEnd != null) {
				dbQuery = dbQuery.Where(r => r.House <= HouseNumberEnd);
			}

			//Четность домов
			if (HouseNumberType != null) {
				var result = HouseNumberType.Value == Queries.HouseNumberType.Even ? 0 : 1;
				dbQuery = dbQuery.Where(r => LinqEx.Mod(r.House, 2) == result);
			}

			RowsCount = dbQuery.Count();
			if (RowsCount > 0) {
				var getCount = RowsCount - PageSize * CurrentPage < PageSize ? RowsCount - PageSize * CurrentPage : PageSize;
				var result = dbQuery.ToList();
				if (!string.IsNullOrEmpty(SortBy))
					result.Sort(
						new PropertyComparer<Request>(IsDesc() ? Common.Tools.SortDirection.Desc : Common.Tools.SortDirection.Asc, SortBy));
				return result.Skip(PageSize * CurrentPage)
					.Take(getCount)
					.ToList();
			}
			return new List<Request>();
		}
	}
}