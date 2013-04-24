using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class AgentFilter : IPaginable
	{
		public uint agent { get; set; }
		public DateTime? startDate { get; set; }
		public DateTime? endDate { get; set; }
		public string year { get; set; }

		[Description("Бонусные")]
		public VirtualType? Virtual { get; set; }

		public int _lastRowsCount;
		public decimal TotalSum;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 20; }
		}

		public int CurrentPage { get; set; }

		public AgentFilter()
		{
			Virtual = null;
		}

		public string[] ToUrl()
		{
			return new[] {
				String.Format("filter.agent={0}", agent),
				String.Format("filter.startDate={0}", startDate),
				String.Format("filter.endDate={0}", endDate),
				String.Format("filter.year={0}", year)
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

		public List<Payment> Find(ISession session)
		{
			var thisD = DateTime.Now;
			if (startDate == null)
				startDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;
			if (!CategorieAccessSet.AccesPartner("SSI"))
				agent = Agent.GetByInitPartner().Id;
			var totalRes = agent > 0 ?
				session.Query<Payment>().Where(t => t.Agent.Id == agent).ToList() : Payment.FindAll().ToList();
			totalRes = totalRes.Where(t => t.PaidOn >= startDate.Value &&
				t.PaidOn <= endDate.Value.AddHours(23).AddMinutes(59) && t.Sum != 0 &&
				t.Client.PhysicalClient != null).ToList();
			if(Virtual != null) {
				totalRes = totalRes.Where(t => t.Virtual == (Virtual == VirtualType.Bonus)).ToList();
			}
			_lastRowsCount = totalRes.Count();
			TotalSum = totalRes.Sum(h => h.Sum);
			if (_lastRowsCount > 0) {
				var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
				return
					totalRes.GetRange(PageSize * CurrentPage, getCount);
			}
			return new List<Payment>();
		}
	}
}