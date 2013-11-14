using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
using ExcelLibrary.BinaryFileFormat;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public enum VirtualType
	{
		[Description("Небонусные")] NoBonus = 0,
		[Description("Бонусные")] Bonus = 1
	}

	public class AgentFilter : PaginableSortable
	{
		public decimal TotalSum;
		public Agent CurrentAgent;
		public Partner CurrentPartner;

		public AgentFilter()
		{
			Year = DateTime.Today.Year.ToString();
			Begin = DateTime.Today.FirstDayOfMonth();
			End = DateTime.Today;
			SortKeyMap = new Dictionary<string, string> {
				{ "PaidOn", "PaidOn" },
			};
		}

		public Agent Agent { get; set; }
		public DateTime Begin { get; set; }
		public DateTime End { get; set; }
		public string Year { get; set; }

		public int Month
		{
			get
			{
				if (Begin.Month == End.Month)
					return Begin.Month;
				return 0;
			}
		}

		public string[] Years
		{
			get
			{
				return Enumerable.Range(2010, DateTime.Now.Year - 2010 + 1)
					.Select(i => i.ToString())
					.ToArray();
			}
		}

		[Description("Бонусные")]
		public VirtualType? Virtual { get; set; }

		public IList<Payment> Find(ISession session)
		{
			if (!CurrentPartner.AccesedPartner.Contains("SSI"))
				Agent = CurrentAgent;

			var begin = Begin;
			var end = End.AddDays(1);

			var query = DetachedCriteria.For<Payment>()
				.Add(Expression.Where<Payment>(p => p.PaidOn >= begin && p.PaidOn <= end));

			if (Agent != null) {
				query.Add(Expression.Where<Payment>(p => p.Agent == Agent));
			}

			if (Virtual != null) {
				query.Add(Expression.Where<Payment>(p => p.Virtual == (Virtual == VirtualType.Bonus)));
			}

			TotalSum = CriteriaTransformer.Clone(query)
				.SetProjection(Projections.Sum<Payment>(p => p.Sum))
				.GetExecutableCriteria(session)
				.UniqueResult<decimal?>() ?? 0;

			return Find<Payment>(session, query).ToList();
		}
	}
}