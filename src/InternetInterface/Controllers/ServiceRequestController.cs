using System;
using System.Collections.Generic;
using System.Threading;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using System.Linq;
#if !DEBUG
using InternetInterface.Helpers;
#endif
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	[Helper(typeof(PaginatorHelper))]
	public class ServiceRequestController : InternetInterfaceController
	{
		public ServiceRequestController()
		{
			SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);
		}

		public void Timetable(DateTime? date, uint? id)
		{
			CancelLayout();
			if (date == null || id == null) {
				PropertyBag["timetable"] = Enumerable.Empty<Timeunit>().ToList();
				return;
			}

			var partner = DbSession.Load<Partner>(id.Value);
			var begin = date.Value.Date;
			var end = date.Value.Date.AddDays(1);
			var requests = DbSession.Query<ServiceRequest>().Where(r => r.PerformanceDate >= begin
				&& r.PerformanceDate < end
				&& r.Performer == partner
				&& r.Status == ServiceRequestStatus.New)
				.ToList();
			PropertyBag["timetable"] = Timeunit.FromRequests(date.Value, partner, requests);
		}

		public void RegisterServiceRequest(uint clientCode)
		{
			var client = DbSession.Load<Client>(clientCode);
			var request = new ServiceRequest(Partner) {
				Client = client
			};
			PropertyBag["request"] = request;
			PropertyBag["ingeners"] = Partner.GetServiceEngineers(DbSession);
			PropertyBag["requests"] = new ServiceRequestFilter(client).Find(DbSession);
			if (IsPost) {
				BindObjectInstance(request, "Request", AutoLoadBehavior.NewInstanceIfInvalidKey);

				if (IsValid(request)) {
					DbSession.Save(request);
					var sms = request.GetNewSms();
					if (SendSms(request, sms))
						Notify("Сохранено");
					RedirectTo(client);
				}
			}
		}

		public void ViewRequests([SmartBinder("filter")] ServiceRequestFilter filter)
		{
			PropertyBag["requests"] = filter.Find(DbSession);
			PropertyBag["engineers"] = Partner.GetServiceEngineers(DbSession);
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
			PropertyBag["ingeners"] = Partner.GetServiceEngineers(DbSession);
			PropertyBag["iteration"] = new ServiceIteration(request);
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

				if (SendSms(request, request.GetEditSms(DbSession)))
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

		private bool SendSms(ServiceRequest request, IEnumerable<SmsMessage> messages)
		{
			if (messages == null)
				return true;

			foreach (var message in messages) {
				SendSms(request, message);
			}
			return !messages.Any(m => m.IsFaulted);
		}

		private bool SendSms(ServiceRequest request, SmsMessage message)
		{
			if (message == null)
				return true;
			SmsHelper.SendMessage(message);
			if (message.IsFaulted) {
				Error("В данный момент отправка SMS инженеру невозможна. Необходимо передать информацию устно.");
				this.Mailer<Mailer>().SmsSendUnavailable(request).Send();
			}
			return message.IsFaulted;
		}
	}
}