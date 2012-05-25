using System;
using System.Collections.Generic;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;

namespace InternetInterface.Queries
{
	public class RequestFinderFilter : Sortable, IPaginable
	{
		public int Status { get; set; }
		public DatePeriod Period { get; set; }
		public Client _Client { get; set; }
		public int DateSelector { get; set; }
		public bool FreeFlag { get; set; }

		private int _lastRowsCount;
		public bool IsService;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 30; }
		}

		public int CurrentPage { get; set; }

		public RequestFinderFilter()
		{
			SortBy = "RegistrationDate";
			SortDirection = "Desc";

			SortKeyMap = new Dictionary<string, string> {
				{"RequestId", "Id"},
				{"ClientId", "Client.Id"},
				{"Description", "Description"},
				{"Contact", "Contact"},
				{"Status", "Status"}
			};
			var dtn = DateTime.Now;
			Period = new DatePeriod(new DateTime(dtn.Year, dtn.Month, 1), dtn);
			IsService = InitializeContent.Partner.CategorieIs("Service");
		}

		public RequestFinderFilter(Client client)
			: this()
		{
			Period = null;
			_Client = client;
		}

		private IList<ServiceRequest> AcceptPaginator(ISession session, DetachedCriteria criteria)
		{
			var countSubquery = CriteriaTransformer.TransformToRowCount(criteria);
			_lastRowsCount = countSubquery.GetExecutableCriteria(session).UniqueResult<int>();

			if (CurrentPage > 0)
				criteria.SetFirstResult(CurrentPage*PageSize);

			criteria.SetMaxResults(PageSize);

			ApplySort(criteria);

			return criteria.GetExecutableCriteria(session).List<ServiceRequest>();
		}

		public DetachedCriteria GetCriteria()
		{
			var criteria = DetachedCriteria.For<ServiceRequest>();
			if (IsService)
				criteria.Add(Expression.Eq("Performer", InitializeContent.Partner));
			if (_Client != null)
				criteria.Add(Expression.Eq("Client", _Client));

			var dateSelectorField = string.Empty;
			switch (DateSelector) {
				case 0:
					dateSelectorField = "RegDate";
					break;
				case 1: 
					dateSelectorField = "PerformanceDate";
					break;
				case 2:
					dateSelectorField = "ClosedDate";
					break;
			}
			if (Status > 0)
				criteria.Add(Restrictions.Eq("Status", (ServiceRequestStatus)Status));
			if (Period != null) {
				criteria.Add(Restrictions.Ge(dateSelectorField, Period.Begin));
				criteria.Add(Restrictions.Le(dateSelectorField, Period.End));
				if (FreeFlag)
					criteria.Add(Restrictions.Eq("Free", FreeFlag));
			}

			return criteria;
		}

		public IList<ServiceRequest> Find(ISession session)
		{
			return AcceptPaginator(session, GetCriteria());
		}
	}
}