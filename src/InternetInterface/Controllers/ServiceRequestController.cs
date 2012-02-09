using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
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
	}
}