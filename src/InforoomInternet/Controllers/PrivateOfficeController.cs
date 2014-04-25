using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using Castle.Components.Binder;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools.Calendar;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InforoomInternet.Models;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AccessFilter))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class PrivateOfficeController : BaseController
	{
		private IPAddress GetHost()
		{
			var hostAdress = Request.UserHostAddress;
#if DEBUG
			if (ConfigurationManager.AppSettings["DebugHost"] != null)
				hostAdress = ConfigurationManager.AppSettings["DebugHost"];
#endif
			return IPAddress.Parse(hostAdress);
		}

		public void AboutSale()
		{
			PropertyBag["saleSettings"] = DbSession.Query<SaleSettings>().First();
		}

		public void BalanceInfo()
		{
			PropertyBag["client"] = LoadClient().PhysicalClient;
		}

		public void IndexOffice(string grouped)
		{
			var client = LoadClient();
			if (client.NeedShowFirstLunchPage(GetHost(), DbSession)) {
				RedirectToAction("FirstVisit");
				return;
			}
			if (!client.FirstLunch) {
				client.FirstLunch = true;
				DbSession.Save(client);
			}

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

		public void FirstVisit()
		{
			var client = LoadClient();
			PropertyBag["client"] = client;
			PropertyBag["PhysicalClient"] = client.PhysicalClient;
			if (IsPost) {
				SetSmartBinder(AutoLoadBehavior.Always);
				BindObjectInstance(client.PhysicalClient, "PhysicalClient");
				if (IsValid(client.PhysicalClient)) {
					var address = GetHost();
					if (client.NoEndPoint() && !ClientEndpoint.HavePoint(DbSession, address)) {
						client.CreateAutoEndPont(address, DbSession);
					}
					client.FirstLunch = true;
					client.Disabled = client.Balance <= 0;
					client.AutoUnblocked = true;
					if (client.IsChanged(c => c.Disabled))
						client.CreareAppeal("Клиент был заблокирован из личного кабинета при посещении первой страницы", AppealType.Statistic);
					DbSession.Save(client);
					Notify("Спасибо, теперь вы можете продолжить работу");
					RedirectToAction("IndexOffice");
				}
			}
		}

		public void PostponedPayment()
		{
			var client = LoadClient();
			PropertyBag["Client"] = client;
			var message = string.Empty;
			if (client.ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof(DebtWork))))
				message += "Повторное использование услуги \"Обещанный платеж\" невозможно";
			if (!client.Disabled && string.IsNullOrEmpty(message))
				message += "Воспользоваться услугой возможно только при отрицательном балансе";
			if ((!client.Disabled || !client.AutoUnblocked) && string.IsNullOrEmpty(message))
				message += "Услуга \"Обещанный платеж\" недоступна";
			if (!client.PaymentForTariff())
				message += "Воспользоваться услугой возможно только при наличии платежей, в сумме равных или превышающих абонентскую плату за месяц";
			PropertyBag["message"] = message;
		}

		public void PostponedPaymentActivate(int daysCount)
		{
			var client = LoadClient();
			var validDayCount = (daysCount == 3) || (daysCount == 10);
			if (client.CanUsedPostponedPayment() & validDayCount) {
				var service = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(daysCount),
					Service = Service.GetByType(typeof(DebtWork))
				};
				Notify(client.Activate(service));
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
			}
			RedirectToUrl("IndexOffice");
		}

		[AccessibleThrough(Verb.Post)]
		public void VoluntaryBlockinActivate(DateTime endDate)
		{
			var client = LoadClient();
			if (client.CanUsedVoluntaryBlockin()) {
				var service = new ClientService {
					BeginWorkDate = DateTime.Now,
					EndWorkDate = endDate,
					Client = client,
					Service = Service.GetByType(typeof(VoluntaryBlockin))
				};
				Notify(client.Activate(service));
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
			}
			RedirectToUrl("IndexOffice");
		}

		[AccessibleThrough(Verb.Post)]
		public void DiactivateVoluntaryBlockin()
		{
			var client = LoadClient();
			var service = client.FindService<VoluntaryBlockin>();
			if (service != null) {
				Notify(client.Deactivate(service));
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
			}
			RedirectToUrl("IndexOffice");
		}

		public void Services()
		{
			var client = LoadClient();
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

					DbSession.Save(client);
					Notify("Сохранено");
					RedirectToReferrer();
				}
			}
		}

		public void SmsNotification(string telephoneInput, bool SendSmsNotifocation)
		{
			var client = LoadClient();
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
						DbSession.Save(smsNotContact);
					}
					else {
						var contact = new Contact(client, ContactType.SmsSending, telNum) {
							Comment = "Пользователь создал из личного кабинета",
						};
						DbSession.Save(contact);
					}
				}
				PropertyBag["telephoneNum"] = telephoneInput;
				if (client.SendSmsNotifocation != SendSmsNotifocation) {
					var message = SendSmsNotifocation ? "Пользователь подписался на sms рассылку" : "Пользователь отписался от sms рассылки";
					DbSession.Save(new Appeals(message, client, AppealType.System));
					client.SendSmsNotifocation = SendSmsNotifocation;
				}
				PropertyBag["SendSmsNotifocation"] = SendSmsNotifocation;
				DbSession.Save(client);
			}
		}

		public void BonusProgram()
		{
		}

		private string TransformTelNum(string num)
		{
			return "8-" + num.Substring(0, 3) + "-" + num.Substring(3, 3) + "-" + num.Substring(6, 2) + "-" + num.Substring(8, 2);
		}

		private Client LoadClient()
		{
			var clientId = Convert.ToUInt32(Session["LoginClient"]);
			var client = DbSession.Load<Client>(clientId);
			return client;
		}
	}
}