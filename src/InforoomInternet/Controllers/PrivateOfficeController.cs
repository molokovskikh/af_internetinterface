using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Components.Binder;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AccessFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class PrivateOfficeController : BaseController
	{
		public void AboutSale()
		{
			PropertyBag["saleSettings"] = SaleSettings.FindFirst();
		}

		public void BalanceInfo()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId).PhysicalClient;
			PropertyBag["client"] = client;
		}

		public void IndexOffice(string grouped)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			PropertyBag["PhysClientName"] = string.Format("{0} {1}", client.PhysicalClient.Name, client.PhysicalClient.Patronymic);
			PropertyBag["PhysicalClient"] = client.PhysicalClient;
			PropertyBag["Client"] = client;
			var writeOffs = client.GetWriteOffs(DbSession, grouped, true).OrderByDescending(e => e.WriteOffDate).ToList();
			if (client.RatedPeriodDate != null) {
				PropertyBag["VisibleWriteOffs"] = writeOffs.Where(w => w.WriteOffDate.Date >= client.RatedPeriodDate.Value.Date).ToList();
				PropertyBag["HideWriteOffs"] = writeOffs.Where(w => w.WriteOffDate.Date < client.RatedPeriodDate.Value.Date).ToList();
			}
			else {
				PropertyBag["VisibleWriteOffs"] = writeOffs.Take(10).ToList();
				PropertyBag["HideWriteOffs"] = writeOffs.Skip(10).ToList();
			}

			PropertyBag["grouped"] = grouped;
			var message = DbSession.Query<MessageForClient>().FirstOrDefault(m => m.Client == client && m.Enabled && m.EndDate > DateTime.Now && !m.Client.Disabled);
			if (message != null)
				PropertyBag["PrivateMessage"] = AppealHelper.GetTransformedAppeal(message.Text);
			PropertyBag["Payments"] =
				Payment.FindAllByProperty("Client", client).Where(p => p.Sum != 0).OrderByDescending(e => e.PaidOn).ToArray();
			if (client.StartNoBlock != null)
				PropertyBag["fullMonth"] = DateTime.Now.TotalMonth(client.StartNoBlock.Value);
			else {
				PropertyBag["fullMonth"] = null;
			}
			if (Context.Session["autoIn"] != null)
				PropertyBag["autoIn"] = true;
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

		[AccessibleThrough(Verb.Post)]
		public void PostponedPaymentActivate(DateTime endDate, decimal debtWorkSum)
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = Client.Find(clientId);
			var haveError = false;
			if (endDate.Date > DateTime.Now.Date.AddDays(3) || endDate.Date < DateTime.Now.Date) {
				Flash["message"] = "Вы не можете установить дату отсрочки платежа более, чем на 3 дня.";
				haveError = true;
			}
			if (endDate.Date == DateTime.MinValue) {
				Flash["message"] = "Должна быть выставлена дата отсрочки платежа";
				haveError = true;
			}
			var minPayment = client.GetPrice() / client.GetInterval();
			if ((debtWorkSum < minPayment) || (debtWorkSum > 10000)) {
				if (haveError)
					Flash["message"] += "<br/>";
				Flash["message"] += string.Format("Сумма обещанного платежа не может менее {0} и более 10000 руб.", minPayment.ToString("0.00"));
				haveError = true;
			}
			if (haveError) {
				RedirectToReferrer();
				//RedirectToUrl("Services");
				return;
			}
			var dtn = DateTime.Now;
			if (client.CanUsedPostponedPayment()) {
				Flash["message"] = "Услуга \"Обещанный платеж активирована\"";
				var CService = new ClientService {
					BeginWorkDate = dtn,
					Client = client,
					EndWorkDate = endDate.AddHours(dtn.Hour).AddMinutes(dtn.Minute).AddSeconds(dtn.Second),
					Service = Service.GetByType(typeof(DebtWork))
				};
				CService.DebtInfo = new DebtWorkInfo(CService, debtWorkSum);
				client.ClientServices.Add(CService);
				CService.Activate();
				new Appeals {
					Appeal = "Услуга \"Обещанный платеж активирована\"",
					AppealType = AppealType.Statistic,
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
					AppealType = AppealType.Statistic,
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
			var cService = client.ClientServices.FirstOrDefault(c => c.Service.Id == Service.GetByType(typeof(VoluntaryBlockin)).Id);
			if (cService != null) {
				cService.CompulsoryDeactivate();
				Flash["message"] = "Услуга \"Добровольная блокировка\" деактивирована";
				new Appeals {
					Appeal = string.Format("Услуга \"добровольная блокировка\" деактивирована"),
					AppealType = AppealType.Statistic,
					Client = client,
					Date = DateTime.Now
				}.Save();
			}
			RedirectToUrl("IndexOffice");
		}

		public void Services()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = DbSession.Load<Client>(clientId);
			var rules = DbSession.Query<TariffChangeRule>().ToList();

			var internet = client.Internet;
			var iptv = client.Iptv;

			var tariffs = DbSession.Query<Tariff>().Where(t => t.CanUseForSelfConfigure).ToList()
				.Concat(new[] { client.PhysicalClient.Tariff }.Where(t => t != null))
				.Distinct()
				.OrderBy(t => t.Name)
				.ToList();

			var channels = DbSession.Query<ChannelGroup>().Where(c => !c.Hidden).ToList()
				.Concat(iptv.Channels)
				.Distinct()
				.OrderBy(c => c.Name)
				.ToList();

			PropertyBag["internet"] = internet;
			PropertyBag["iptv"] = iptv;
			PropertyBag["tariffs"] = tariffs;
			PropertyBag["channels"] = channels;
			PropertyBag["Client"] = client;
			PropertyBag["VoluntaryBlockinService"] = new VoluntaryBlockin();

			if (IsPost) {
				if (!client.CanEditServicesFromPrivateOffice) {
					Error("Услуги можно редактировать только когда баланс положительный и клиент активен");
					RedirectToReferrer();
					return;
				}

				SetSmartBinder(AutoLoadBehavior.NullIfInvalidKey);
				BindObjectInstance(internet, "internet", "ActivatedByUser");
				BindObjectInstance(client, "client", "PhysicalClient.Tariff");

				if (IsValid(client.PhysicalClient)) {
					client.PhysicalClient.UpdatePackageId();
					client.PhysicalClient.WriteOffIfTariffChanged(rules);

					//может не быть ни одного канала и тогда биндер сломается
					if (Request.ObtainParamsNode(ParamStore.Params).GetChildNode("iptv") != null) {
						var updatedChannels = BindObject<List<ChannelGroup>>("iptv.Channels");
						iptv.UpdateChannels(updatedChannels);
					}

					DbSession.SaveOrUpdate(client);
					Notify("Сохранено");
					RedirectToReferrer();
				}
			}
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
				var smsNotContact = client.Contacts.FirstOrDefault(c => c.Type == ContactType.SmsSending);
				if (smsNotContact != null)
					PropertyBag["telephoneNum"] = TransformTelNum(smsNotContact.Text);
			}
			if (IsPost) {
				var telNum = telephoneInput.Replace("-", string.Empty).Remove(0, 1);
				if (client.Contacts != null) {
					var smsNotContact = client.Contacts.FirstOrDefault(c => c.Type == ContactType.SmsSending);
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

		public void BonusProgram()
		{
		}
	}
}