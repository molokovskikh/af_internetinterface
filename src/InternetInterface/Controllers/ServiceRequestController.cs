using System;
using System.Collections.Generic;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
using System.Linq;

#if !DEBUG
using InternetInterface.Helpers;
#endif

namespace InternetInterface.Controllers
{
	public class InternetInterfaceController : BaseController
	{
		protected void RedirectTo(Client client)
		{
			string uri;
			if (client.GetClientType() == ClientType.Phisical) {
				uri = "~/UserInfo/SearchUserInfo.rails?filter.ClientCode={0}";
			}
			else {
				uri = "~/UserInfo/LawyerPersonInfo.rails?filter.ClientCode={0}";
			}
			RedirectToUrl(string.Format(uri, client.Id));
		}

		public SmsHelper SmsHelper = new SmsHelper();

		protected Partner Partner
		{
			get { return InitializeContent.Partner; }
		}
	}

	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	[Helper(typeof(PaginatorHelper))]
	public class ServiceRequestController : InternetInterfaceController
	{
		public ServiceRequestController()
		{
			SetBinder(new ARDataBinder());
		}

		public void RegisterServiceRequest(uint clientCode)
		{
			var client = DbSession.Load<Client>(clientCode);
			var request = new ServiceRequest { Registrator = Partner };
			PropertyBag["client"] = client;
			PropertyBag["request"] = request;
			PropertyBag["ingeners"] = Partner.GetServiceIngeners();
			PropertyBag["requests"] = new RequestFinderFilter(client).Find(DbSession);
			if (IsPost) {
				BindObjectInstance(request, "Request", AutoLoadBehavior.NewInstanceIfInvalidKey);

				if (IsValid(request)) {
					DbSession.Save(request);
					var sms = request.GetSms();
					if (SendSms(sms, request))
						Notify("Сохранено");
					RedirectTo(client);
				}
			}
		}

		public void ViewRequests([DataBind("filter")] RequestFinderFilter filter)
		{
			PropertyBag["requests"] = filter.Find(DbSession);
			PropertyBag["filter"] = filter;
			PropertyBag["IsService"] = filter.IsService;
		}

		public void ShowRequest(uint id, bool edit)
		{
			var request = DbSession.Load<ServiceRequest>(id);
			var isService = Partner.CategorieIs("Service");
			PropertyBag["Request"] = ((isService && request.Performer == Partner) || !isService) ? request : null;
			PropertyBag["Edit"] = edit;
			PropertyBag["IsService"] = Partner.CategorieIs("Service");
			if (edit) {
				PropertyBag["ingeners"] = Partner.GetServiceIngeners();
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AddIteration()
		{
			var iteration = new ServiceIteration();
			BindObjectInstance(iteration, "iteration", AutoLoadBehavior.NewInstanceIfInvalidKey);
			if (iteration.Request != null)
				DbSession.Save(iteration);
			RedirectToReferrer();
		}

		[AccessibleThrough(Verb.Post)]
		public void EditServiceRequest([ARDataBind("Request", AutoLoadBehavior.NullIfInvalidKey)] ServiceRequest request)
		{
			if (request != null) {
				var writeOff = request.GetWriteOff(DbSession);
				DbSession.Save(request);
				if (writeOff != null)
					DbSession.Save(writeOff);

				if (SendSms(request.GetEditSms(DbSession), request))
					Notify("Сохранено");

				RedirectToUrl("../ServiceRequest/ShowRequest?Id=" + request.Id);
			}
			else
				RedirectToReferrer();
		}

		public void AddServiceComment(uint requestId, string commentText)
		{
			commentText = string.Format("Заявка стала бесплатной, поскольку: {0}", commentText);
			var request = DbSession.Load<ServiceRequest>(requestId);
			var interaction = new ServiceIteration {
				Request = request,
				Description = commentText
			};
			DbSession.Save(interaction);
			CancelView();
			CancelLayout();
		}

		private bool SendSms(SmsMessage sms, ServiceRequest request)
		{
			if (sms == null)
				return true;

			SmsHelper.SendMessage(sms);
			if (sms.IsFaulted) {
				Error("В данный момент отправка SMS инженеру невозможна. Необходимо передать информацию устно.");
				this.Mailer<Mailer>().SmsSendUnavailable(request).Send();
				return false;
			}
			return true;
		}
	}
}