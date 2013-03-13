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
	public class ContractFilter : PaginableSortable
	{
		public string SearchText { get; set; }

		public ContractFilter()
		{
			SortBy = "Date";
			SortDirection = "desc";
			SortKeyMap = new Dictionary<string, string> {
				{ "Id", "Id" },
				{ "Date", "Date" },
				{ "Customer", "Customer" },
				{ "Number", "c.Number" }
			};
		}

		public IList<Contract> Find(ISession session)
		{
			var criteria = DetachedCriteria.For<Contract>()
				.CreateAlias("Order", "c");
			if (!string.IsNullOrEmpty(SearchText)) {
				uint id;
				if (uint.TryParse(SearchText, out id))
					criteria.Add(Expression.Eq("c.ClientId", id));
				else
					criteria.Add(Expression.Like("Customer", SearchText, MatchMode.Anywhere));
			}

			return Find<Contract>(criteria);
		}
	}
}