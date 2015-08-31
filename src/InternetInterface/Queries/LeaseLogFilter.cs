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
		public LeaseLogFilter()
		{
			beginDate = DateTime.Today.FirstDayOfMonth();
			endDate = DateTime.Today;
		}

		public uint ClientCode { get; set; }
		public DateTime beginDate { get; set; }
		public DateTime endDate { get; set; }
		public int RowsCount { get; private set; }

		public int PageSize
		{
			get { return 20; }
		}

		public int CurrentPage { get; set; }

		public string GetUri()
		{
			return ToUrlQuery();
		}

		public string[] ToUrl()
		{
			return new[]
			{
				string.Format("filter.ClientCode={0}", ClientCode),
				string.Format("filter.beginDate={0}", beginDate),
				string.Format("filter.endDate={0}", endDate)
			};
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public List<SessionResult> Find(ISession session)
		{
			var result = new List<SessionResult>();
			//паолучение отключенных эндпойнтов
			var endpointLog = session.Query<ClientEndpointLog>().Where(s => s.Client == ClientCode).ToList();

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

			//получение записей по эндпойнтам клиента
			result = session.Query<Internetsessionslog>().Where(predicate)
				.ToList().Select(i => new SessionResult(i)).ToList();
			//получение записей по отключенным эндпойнтам клиента
			foreach (var itemLog in endpointLog) {
				Expression<Func<Internetsessionslog, bool>> predicate_temp = i =>
					i.EndpointId.Id == itemLog.ClientendpointId
					&& i.LeaseBegin.Date >= beginDate
					&& i.LeaseBegin.Date < endDate.AddDays(1);
				var resultWithItemLog = session.Query<Internetsessionslog>().Where(predicate_temp)
					.ToList().Select(i => new SessionResult(i)).ToList();
				resultWithItemLog = resultWithItemLog.Where(s => !result.Any(f => f.Lease.Id == s.Lease.Id)).ToList();
				result.AddRange(resultWithItemLog);
			}

			RowsCount = result.Count;
			RowsCount += appeal.Count;
			var getCount = 0;
			if (RowsCount > 0) {
				getCount = RowsCount - PageSize * CurrentPage < PageSize ? RowsCount - PageSize * CurrentPage : PageSize;  
			}
			result.AddRange(appeal);
			return result.OrderByDescending(r => r.Date).Skip(PageSize * CurrentPage).Take(getCount).ToList();
		}
	}
}