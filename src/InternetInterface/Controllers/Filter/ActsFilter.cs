using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;

namespace InternetInterface.Controllers.Filter
{
	public class ActsFilter : PaginableSortable
	{
		public string SearchText { get; set; }

		[Description("Выберите год:")]
		public int? Year { get; set; }

		[Description("Выберите период:")]
		public Interval? Interval { get; set; }

		public ActsFilter()
		{
			SortBy = "ActDate";
			SortDirection = "desc";
			SortKeyMap = new Dictionary<string, string> {
				{ "Id", "Id" },
				{ "ClientId", "c.Id" },
				{ "Date", "ActDate" },
				{ "Sum", "Sum" },
				{ "Period", "Period" },
				{ "PayerName", "PayerName" }
			};
		}

		public IList<Act> Find(ISession session)
		{
			var criteria = DetachedCriteria.For<Act>()
				.CreateAlias("Client", "c");
			if (!string.IsNullOrEmpty(SearchText)) {
				uint id;
				if (uint.TryParse(SearchText, out id))
					criteria.Add(Expression.Eq("c.Id", id));
				else
					criteria.Add(Expression.Like("PayerName", SearchText, MatchMode.Anywhere));
			}

			if (Year != null)
				criteria.Add(Expression.Sql("{alias}.Period like " + String.Format("'{0}-%'", Year)));

			if (Interval != null)
				criteria.Add(Expression.Sql("{alias}.Period like " + String.Format("'%-{0}'", (int)Interval)));

			return Find<Act>(criteria);
		}
	}
}