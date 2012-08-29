using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Queries
{
	public class RequestFilter : IPaginable, ISortableContributor, SortableContributor
	{
		public string query { get; set; }
		public DateTime? beginDate { get; set; }
		public DateTime? endDate { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }
		public uint? labelId { get; set; }
		public bool Archive { get; set; }

		private int _lastRowsCount;

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
			return
				GetParameters().Where(p => p.Key != "CurrentPage").Select(p => string.Format("{0}={1}", p.Key, p.Value))
					.ToArray();
		}

		public Dictionary<string, object> GetParameters()
		{
			return new Dictionary<string, object> {
				{ "filter.query", query },
				{ "filter.beginDate", beginDate },
				{ "filter.endDate", endDate },
				{ "filter.labelId", labelId },
				{ "filter.Archive", Archive },
				{ "filter.SortBy", SortBy },
				{ "filter.Direction", Direction },
				{ "CurrentPage", CurrentPage }
			};
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public string GetUriToArchive()
		{
			Archive = !Archive;
			var label = Archive ? "Показать архив" : "Показать заявки";
			var a = string.Format("<a href=\"../UserInfo/RequestView?{0}\">{1}</a>", GetUri(), label);
			Archive = !Archive;
			return a;
		}

		public string GetUri()
		{
			return ToUrlQuery();
		}

		public List<Request> Find()
		{
			var thisD = DateTime.Now;
			if (beginDate == null)
				beginDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;

			Expression<Func<Request, bool>> predicate;
			if (labelId != 0)
				if (!string.IsNullOrEmpty(query))
					predicate = i => (i.Street.Contains(query) || i.ApplicantPhoneNumber.Contains(query) || i.ApplicantName.Contains(query)) && i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Label.Id == labelId && i.Archive == Archive;
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

			_lastRowsCount = Request.Queryable.Where(predicate).Count();
			if (_lastRowsCount > 0) {
				var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
				var result = Request.Queryable.Where(predicate).ToList();
				if (!string.IsNullOrEmpty(SortBy))
					result.Sort(new PropertyComparer<Request>(Direction == "asc" ? SortDirection.Asc : SortDirection.Desc, SortBy));
				return result.Skip(PageSize * CurrentPage)
					.Take(getCount)
					.ToList();
			}
			return new List<Request>();
		}
	}
}