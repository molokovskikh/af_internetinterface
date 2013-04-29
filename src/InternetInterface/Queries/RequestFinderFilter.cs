using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using Order = NHibernate.Criterion.Order;

namespace InternetInterface.Queries
{
	public class RequestFinderFilter : PaginableSortable
	{
		[Description("Статус")]
		public ServiceRequestStatus? Status { get; set; }

		public DatePeriod Period { get; set; }
		public Client _Client { get; set; }
		public int DateSelector { get; set; }
		public string Text { get; set; }

		[Description("Бесплатные")]
		public bool FreeFlag { get; set; }

		public bool IsService;

		public RequestFinderFilter()
		{
			PageSize = 30;
			SortBy = "RegistrationDate";
			SortDirection = "Desc";

			SortKeyMap = new Dictionary<string, string> {
				{ "RequestId", "Id" },
				{ "ClientId", "Client.Id" },
				{ "Description", "Description" },
				{ "Contact", "Contact" },
				{ "RegDate", "RegDate" },
				{ "ClosedDate", "ClosedDate" },
				{ "Sum", "Sum" },
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
			if (Status != null)
				criteria.Add(Restrictions.Eq("Status", Status));
			if (Period != null) {
				criteria.Add(Restrictions.Ge(dateSelectorField, Period.Begin));
				criteria.Add(Restrictions.Le(dateSelectorField, Period.End.AddDays(1)));
				if (FreeFlag)
					criteria.Add(Restrictions.Eq("Free", FreeFlag));
			}
			if (!string.IsNullOrEmpty(Text)) {
				uint clientId = 0;
				if (UInt32.TryParse(Text, out clientId)) {
					criteria.CreateAlias("Client", "CL", JoinType.InnerJoin);
					criteria.Add(Restrictions.Eq("CL.Id", clientId));
				}
				else {
					criteria.Add(Restrictions.Like("Description", Text, MatchMode.Anywhere));
				}
			}
			criteria.AddOrder(Order.Asc("Status"));
			return criteria;
		}

		public IList<ServiceRequest> Find(ISession session)
		{
			return Find<ServiceRequest>(GetCriteria());
		}
	}
}