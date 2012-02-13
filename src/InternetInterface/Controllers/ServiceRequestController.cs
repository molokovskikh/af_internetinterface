﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using Expression = NHibernate.Criterion.Expression;

namespace InternetInterface.Controllers
{
	public class RequestFinderFilter : Sortable, IPaginable
	{
		public int Status { get; set; }
		public DatePeriod Period { get; set; }
		public Client _Client { get; set; }
		public int DateSelector { get; set; }

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

		private IList<ServiceRequest> AcceptPaginator(DetachedCriteria criteria)
		{
			var countSubquery = CriteriaTransformer.TransformToRowCount(criteria);
			_lastRowsCount = ArHelper.WithSession(s => countSubquery.GetExecutableCriteria(s).UniqueResult<int>());

			if (CurrentPage > 0)
				criteria.SetFirstResult(CurrentPage*PageSize);

			criteria.SetMaxResults(PageSize);

			ApplySort(criteria);

			return ArHelper.WithSession(s => criteria.GetExecutableCriteria(s).List<ServiceRequest>()).ToList();
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
			}

			return criteria;
		}

		public IList<ServiceRequest> Find()
		{
			var result = AcceptPaginator(GetCriteria());

			return result;
		}
	}


	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	[Helper(typeof(PaginatorHelper))]
	public class ServiceRequestController : ARSmartDispatcherController
	{
		public void RegisterServiceRequest(uint clientCode)
		{
			var client = Client.Find(clientCode);
			PropertyBag["client"] = client;
			PropertyBag["ingeners"] = Partner.GetServiceIngeners();
			if (IsPost) {
				var newRequest = new ServiceRequest();
				BindObjectInstance(newRequest, "request", AutoLoadBehavior.NewInstanceIfInvalidKey);
				newRequest.Save();
				var message = new SmsMessage {
					PhoneNumber = "+7" + newRequest.Performer.TelNum,
					Text = string.Format("Зарегистрирован вызов к клиенту. Заявка №{0}, клиент №{1}", newRequest.Id, client.Id)
				};
#if !DEBUG
				SmsHelper.SendMessage(message);
#endif
				Flash["Message"] = "Сохранено";
				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + client.Id);
			}
		}

		public void ViewRequests([DataBind("filter")]RequestFinderFilter filter)
		{
			PropertyBag["requests"] = filter.Find();
			PropertyBag["filter"] = filter;
			PropertyBag["IsService"] = filter.IsService;
			PropertyBag["Statuses"] = ServiceRequest.GetStatuses();
		}

		public void ShowRequest(uint Id, bool Edit)
		{
			var request = ServiceRequest.Find(Id);
			var isService = InitializeContent.Partner.CategorieIs("Service");
			PropertyBag["request"] = ((isService && request.Performer == InitializeContent.Partner) || ! isService) ? request : null;
			PropertyBag["Edit"] = Edit;
			PropertyBag["IsService"] = InitializeContent.Partner.CategorieIs("Service");
			if (Edit) {
				PropertyBag["RequestStatuses"] = ServiceRequest.GetStatuses();
				PropertyBag["ingeners"] = Partner.GetServiceIngeners();
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AddIteration()
		{
			var iteration = new ServiceIteration();
			BindObjectInstance(iteration, "iteration", AutoLoadBehavior.NewInstanceIfInvalidKey);
			if (iteration.Request != null)
				iteration.Save();
			RedirectToReferrer();
		}

		[AccessibleThrough(Verb.Post)]
		public void EditServiceRequest([ARDataBind("request", AutoLoadBehavior.NullIfInvalidKey)]ServiceRequest request)
		{
			if (request != null){
				InitializeHelper.InitializeModel(request);
				request.Save();
				RedirectToUrl("../ServiceRequest/ShowRequest?Id=" + request.Id);
			}
			else
				RedirectToReferrer();
		}

		public void AddServiceComment()
		{
			var requestId = Convert.ToUInt32(Request.Params["requestId"]);
			var commentText = string.Format("Заявка стала бесплатной, поскольку: {0}", Request.Params["commentText"]);
			new ServiceIteration {
				Request = ServiceRequest.Find(requestId),
				Description = commentText
			}.Save();
		}
	}
}