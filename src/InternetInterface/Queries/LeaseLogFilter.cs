using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class LeaseLogFilter : IPaginable
	{
		public uint ClientCode { get; set; }
		public DateTime beginDate { get; set; }
		public DateTime endDate { get; set; }

		private int _lastRowsCount;

		public LeaseLogFilter()
		{
			beginDate = DateTime.Today.FirstDayOfMonth();
			endDate = DateTime.Today;
		}

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
				String.Format("filter.ClientCode={0}", ClientCode),
				String.Format("filter.beginDate={0}", beginDate),
				String.Format("filter.endDate={0}", endDate)
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

		public List<SessionResult> Find(ISession session)
		{
			var result = new List<SessionResult>();
			Expression<Func<Internetsessionslog, bool>> predicate = i =>
				i.EndpointId.Client.Id == ClientCode
					&& i.LeaseBegin.Date >= beginDate
					&& i.LeaseBegin.Date < endDate.AddDays(1);

			var appeal = session.Query<Appeals>().Where(a =>
				a.Client.Id == ClientCode &&
					a.AppealType == AppealType.Statistic &&
					a.Date.Date >= beginDate &&
					a.Date.Date < endDate.AddDays(1))
				.ToList().Select(a => new SessionResult(a)).ToList();
			_lastRowsCount = session.Query<Internetsessionslog>().Where(predicate).Count();
			_lastRowsCount += appeal.Count;
			int getCount = 0;
			if (_lastRowsCount > 0) {
				getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
				result = session.Query<Internetsessionslog>().Where(predicate)
					.ToList().Select(i => new SessionResult(i)).ToList();
			}
			result.AddRange(appeal);
			return result.OrderByDescending(r => r.Date)
				.Skip(PageSize * CurrentPage)
				.Take(getCount)
				.ToList();
		}
	}
}