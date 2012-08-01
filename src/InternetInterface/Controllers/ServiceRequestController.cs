using System.Collections.Generic;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using System.Linq;
#if !DEBUG
using InternetInterface.Helpers;
#endif

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	[Helper(typeof(PaginatorHelper))]
	public class ServiceRequestController : BaseController
	{
		public ServiceRequestController()
		{
			SetBinder(new ARDataBinder());
		}

		public void RegisterServiceRequest(uint clientCode)
		{
			var client = Client.Find(clientCode);
			var request = new ServiceRequest{Registrator = InitializeContent.Partner};
			PropertyBag["client"] = client;
			PropertyBag["request"] = request;
			PropertyBag["ingeners"] = Partner.GetServiceIngeners();
			PropertyBag["requests"] = new RequestFinderFilter(client).Find(DbSession);
			if (IsPost) {
				BindObjectInstance(request, "Request", AutoLoadBehavior.NewInstanceIfInvalidKey);

				if (IsValid(request)) {
					DbSession.Save(request);
					var sms = request.GetSms();
					if (sms != null) {
#if !DEBUG
						SmsHelper.SendMessage(sms);
#endif
					}

					Notify("Сохранено");
					RedirectToUrl("~/Search/Redirect?filter.ClientCode=" + client.Id);
				}
			}
		}

		public void ViewRequests([DataBind("filter")] RequestFinderFilter filter)
		{
			PropertyBag["requests"] = filter.Find(DbSession);
			PropertyBag["filter"] = filter;
			PropertyBag["IsService"] = filter.IsService;
			PropertyBag["Statuses"] = ServiceRequest.GetStatuses();
		}

		public void ShowRequest(uint id, bool edit)
		{
			var request = DbSession.Load<ServiceRequest>(id);
			var isService = InitializeContent.Partner.CategorieIs("Service");
			PropertyBag["Request"] = ((isService && request.Performer == InitializeContent.Partner) || ! isService) ? request : null;
			PropertyBag["Edit"] = edit;
			PropertyBag["IsService"] = InitializeContent.Partner.CategorieIs("Service");
			if (edit) {
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
		public void EditServiceRequest([ARDataBind("Request", AutoLoadBehavior.NullIfInvalidKey)] ServiceRequest request)
		{
			if (request != null) {
				DbSession.Save(request);
				if (request.Writeoff != null) {
					DbSession.Save(request.Writeoff);
				}
				RedirectToUrl("../ServiceRequest/ShowRequest?Id=" + request.Id);
			}
			else
				RedirectToReferrer();
		}

		public void AddServiceComment(uint requestId, string commentText)
		{
			commentText = string.Format("Заявка стала бесплатной, поскольку: {0}", commentText);
			var request = DbSession.Load<ServiceRequest>(requestId);
			new ServiceIteration {
				Request = request,
				Description = commentText
			}.Save();
			CancelView();
			CancelLayout();
		}
	}
}