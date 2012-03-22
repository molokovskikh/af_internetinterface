using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AccessFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class PrivateOffice:SmartDispatcherController
	{
		public void AboutSale()
		{
			PropertyBag["saleSettings"] = SaleSettings.FindFirst();
		}

		public void IndexOffice(string grouped)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			PropertyBag["PhysClientName"] = string.Format("{0} {1}", client.PhysicalClient.Name, client.PhysicalClient.Patronymic);
			PropertyBag["PhysicalClient"] = client.PhysicalClient;
			PropertyBag["Client"] = client;
			PropertyBag["WriteOffs"] = client.GetWriteOffs(grouped).OrderBy(e => e.WriteOffDate).ToArray();
			PropertyBag["grouped"] = grouped;
			var message = MessageForClient.Queryable.FirstOrDefault(m => m.Client == client && m.Enabled && m.EndDate > DateTime.Now && !m.Client.Disabled);
			if (message != null)
				PropertyBag["PrivateMessage"] =  AppealHelper.GetTransformedAppeal(message.Text);
			PropertyBag["Payments"] =
				Payment.FindAllByProperty("Client", client).Where(p => p.Sum != 0).OrderBy(e => e.PaidOn).ToArray();
			if (client.StartNoBlock != null)
				PropertyBag["fullMonth"] = DateTime.Now.TotalMonth(client.StartNoBlock.Value);
			else {
				PropertyBag["fullMonth"] = null;
			}
		}

		public void PostponedPayment()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			PropertyBag["Client"] = client;
			var message = string.Empty;
			if (client.ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof(DebtWork))))
				message += "Повторное использование услуги \"Обещаный платеж\" невозможно";
			if (!client.Disabled && string.IsNullOrEmpty(message))
				message += "Воспользоваться устугой возможно только при отрицательном балансе";
			if ((!client.Disabled || !client.AutoUnblocked) && string.IsNullOrEmpty(message))
				message += "Услуга \"Обещанный платеж\" недоступна";
			if (!client.PaymentForTariff())
				message += "Воспользоваться услугой возможно только при наличии платежей, в сумме равных или превышающих абонентскую плату за месяц";
			PropertyBag["message"] = message;
		}

		public void PostponedPaymentActivate()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			if (client.CanUsedPostponedPayment())
			{
				Flash["message"] = "Услуга \"Обещанный платеж активирована\"";
				var CService = new ClientService {
									  BeginWorkDate = DateTime.Now,
									  Client = client,
									  EndWorkDate = DateTime.Now.AddDays(1),
									  Service = Service.GetByType(typeof(DebtWork))
								  };
				client.ClientServices.Add(CService);
				CService.Activate();
				new Appeals {
								Appeal = "Услуга \"Обещанный платеж активирована\"",
								AppealType = AppealType.System,
								Client = client,
								Date = DateTime.Now
							}.Save();
			}
			RedirectToUrl("IndexOffice");
		}

		[AccessibleThrough(Verb.Post)]
		public void VoluntaryBlockinActivate(DateTime endDate)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			if (client.CanUsedVoluntaryBlockin()) {
				//Flash["message"] = "Услуга \"Работа в долг\" активирована";
				var cService = new ClientService {
					BeginWorkDate = DateTime.Now,
					EndWorkDate = endDate,
					Client = client,
					Service = Service.GetByType(typeof(VoluntaryBlockin))
				};
				client.ClientServices.Add(cService);
				cService.Activate();
				new Appeals {
					Appeal = string.Format("Услуга \"добровольная блокировка\" активирована на период с {0} по {1}", DateTime.Now.ToShortDateString(), endDate.ToShortDateString()),
					AppealType = AppealType.System,
					Client = client,
					Date = DateTime.Now
				}.Save();
			}
			RedirectToUrl("IndexOffice");
		}

		[AccessibleThrough(Verb.Post)]
		public void DiactivateVoluntaryBlockin()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			var cService = client.ClientServices.Where(c => c.Service.Id == Service.GetByType(typeof (VoluntaryBlockin)).Id).FirstOrDefault();
			if (cService != null) {
				cService.CompulsoryDiactivate();
				Flash["message"] = "Услуга \"Работа в долг\" деактивирована";
					new Appeals {
					Appeal = string.Format("Услуга \"добровольная блокировка\" деактивирована"),
					AppealType = AppealType.System,
					Client = client,
					Date = DateTime.Now
				}.Save();
			}
			RedirectToUrl("IndexOffice");
		}

		public void Services()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			PropertyBag["Client"] = client;
			PropertyBag["VoluntaryBlockinService"] = new VoluntaryBlockin();
		}

		private string TransformTelNum(string num)
		{
			return "8-" + num.Substring(0, 3) + "-" + num.Substring(3, 3) + "-" + num.Substring(6, 2) + "-" + num.Substring(8, 2);
		}

		public void SmsNotification(string telephoneInput, bool SendSmsNotifocation)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			PropertyBag["telephoneNum"] = string.Empty;
			PropertyBag["SendSmsNotifocation"] = client.SendSmsNotifocation;
			if (client.Contacts != null) {
				var smsNotContact = client.Contacts.Where(c => c.Type == ContactType.SmsSending).FirstOrDefault();
				if (smsNotContact != null)
					PropertyBag["telephoneNum"] = TransformTelNum(smsNotContact.Text);
			}
			if (IsPost) {
				var telNum = telephoneInput.Replace("-", string.Empty).Remove(0, 1);
				if (client.Contacts != null) {
					var smsNotContact = client.Contacts.Where(c => c.Type == ContactType.SmsSending).FirstOrDefault();
					if (smsNotContact != null) {
						smsNotContact.Text = telNum;
						smsNotContact.Save();
					}
					else {
						new Contact {
							Client = client,
							Text = telNum,
							Date = DateTime.Now,
							Type = ContactType.SmsSending,
							Comment = "Пользователь создал из личного кабинета"
						}.Save();
					}
				}
				PropertyBag["telephoneNum"] = telephoneInput;
				if (client.SendSmsNotifocation != SendSmsNotifocation) {
					new Appeals {
						Client = client,
						Date = DateTime.Now,
						AppealType = AppealType.System,
						Appeal = SendSmsNotifocation ? "Пользователь подписался на sms рассылку" : "Пользователь отписался от sms рассылки"
					}.Save();
					client.SendSmsNotifocation = SendSmsNotifocation;
				}
				PropertyBag["SendSmsNotifocation"] = SendSmsNotifocation;
				client.Save();
			}
		}
	}
}