﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using Order = NHibernate.Criterion.Order;

namespace InternetInterface.Queries
{
	public class ServiceRequestFilter : PaginableSortable
	{
		[Description("Статус")]
		public ServiceRequestStatus? Status { get; set; }

		public DatePeriod Period { get; set; }
		public Client _Client { get; set; }
		public int DateSelector { get; set; }
		public string Text { get; set; }

		[Description("Бесплатные")]
		public bool FreeFlag { get; set; }

		[Description("Назначено")]
		public Partner Partner { get; set; }

		[Description("Регион")]
		public RegionHouse Region { get; set; }

		public bool IsService;

		public ServiceRequestFilter()
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
				{ "CancelDate", "CancelDate" },
				{ "Sum", "Sum" },
				{ "Performer", "Performer" },
				{ "PerformanceDate", "PerformanceDate" },
			};
			var dtn = DateTime.Now;
			Period = new DatePeriod(new DateTime(dtn.Year, dtn.Month, 1), dtn);
			IsService = InitializeContent.Partner.CategorieIs("Service");
		}

		public ServiceRequestFilter(Client client)
			: this()
		{
			Period = null;
			_Client = client;
		}

		public DetachedCriteria GetCriteria()
		{
			var criteria = DetachedCriteria.For<ServiceRequest>()
				.CreateAlias("Client", "CL", JoinType.InnerJoin);
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
			if (Partner != null) {
				criteria.Add(Expression.Where<ServiceRequest>(r => r.Performer == Partner));
			}
			if (Region != null) {
				criteria.CreateAlias("CL.PhysicalClient", "p", JoinType.LeftOuterJoin)
					.CreateAlias("p.HouseObj", "h", JoinType.LeftOuterJoin)
					.CreateAlias("CL.LawyerPerson", "l", JoinType.LeftOuterJoin);
				criteria.Add(Restrictions.Eq("h.Region", Region) || Restrictions.Eq("l.Region", Region));
			}
			if (!string.IsNullOrEmpty(Text)) {
				uint id;
				if (UInt32.TryParse(Text, out id)) {
					criteria.Add(Restrictions.Eq("CL.Id", id)
						|| Restrictions.Eq("Id", id)
						|| Restrictions.Like("Contact", Text, MatchMode.Anywhere));
				}
				else {
					criteria.Add(
						Restrictions.Like("Description", Text, MatchMode.Anywhere)
							|| Restrictions.Like("CL.Name", Text, MatchMode.Anywhere)
							|| Restrictions.Like("CL.Address", Text, MatchMode.Anywhere)
							|| Restrictions.Like("Contact", Text, MatchMode.Anywhere));
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