using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
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
					PhoneNumber = InitializeContent.Partner.TelNum,
					Text = string.Format("Зарегистрирован вызов к клиенту. Заявка №{0}, клиент №{1}", newRequest.Id, client.Id)
				};
				/*SmsHelper.SendMessage(message);*/
				Flash["Message"] = "Сохранено";
				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + client.Id);
			}
		}

		public void ViewMyRequest()
		{
			var myRequest = ServiceRequest.Queryable.Where( r => r.Performer == InitializeContent.Partner ).ToList();
			PropertyBag["requests"] = myRequest;
		}

		public void ShowRequest(uint Id, bool Edit)
		{
			var request = ServiceRequest.Find(Id);
			var isService = InitializeContent.Partner.CategorieIs("Service");
			PropertyBag["request"] = ((isService && request.Performer == InitializeContent.Partner) || ! isService) ? request : null;
			PropertyBag["Edit"] = Edit;
			if (Edit) {
				PropertyBag["RequestStatuses"] = Enum.GetValues(typeof(ServiceRequestStatus)).Cast<int>().Select( s => new {Id = s, Name = ServiceRequest.GetStatusName((ServiceRequestStatus)s)});
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
				InitializeHelper.InitializeModel(request.Client);
				request.Save();
				RedirectToUrl("../ServiceRequest/ShowRequest?Id=" + request.Id);
			}
			else
				RedirectToReferrer();
		}
	}
}