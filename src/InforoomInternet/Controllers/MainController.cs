using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Editor;
using InforoomInternet.Models;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class MainController : BaseController
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

		private Lease FindLease()
		{
			return DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == GetHost());
		}

		public void Index()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Zayavka()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Main()
		{
		}

		public void HowPay(bool edit)
		{
			SetEdatableAttribute(edit, "HowPay");
		}

		public void Ok()
		{
			if (Flash["application"] == null)
				RedirectToSiteRoot();
		}

		public void OfferContract(bool edit)
		{
		}

		public void PrivateOffice()
		{
		}

		public void Feedback(string clientName, string contactInfo, uint clientId, string appealText, uint autoClientId)
		{
			var ip = Request.UserHostAddress;
			var mailToAdress = "internet@ivrn.net";
#if DEBUG
			mailToAdress = "kvasovtest@analit.net";
#endif
			var lease = FindLease();
			var client = lease != null && lease.Endpoint != null ? lease.Endpoint.Client : null;
			client = client ?? new Client();
			PropertyBag["client"] = client;

			if (IsPost) {
				var Text = new StringBuilder();
				Text.AppendLine("Обращение клиента через сайт WWW.IVRN.NET \r\n");
				Text.AppendLine("Клиент пришел с адреса: " + ip);
				Text.AppendLine(string.Format("Наша система определила запрос с лицевого счета номер: {0} \r\n", autoClientId));
				Text.AppendLine("Введена информация : \r\n");
				Text.AppendLine("ФИО: " + clientName);
				Text.AppendLine("Контактная информация: " + contactInfo);
				Text.AppendLine("Номер счета: " + clientId);
				Text.AppendLine("Текст обращения: \r\n" + appealText);
				if (!string.IsNullOrEmpty(clientName) && !string.IsNullOrEmpty(contactInfo) && !string.IsNullOrEmpty(appealText)) {
					var userClient = DbSession.Get<Client>(clientId);
					new Appeals {
						Client = userClient,
						Date = DateTime.Now,
						AppealType = AppealType.FeedBack,
						Appeal = Text.ToString()
					}.Save();
					if (userClient != null) {
						contactInfo = contactInfo.Replace("-", string.Empty);
						if (userClient.Contacts != null && !userClient.Contacts.Select(c => c.Text).Contains(contactInfo)) {
							var contact = new Contact(userClient, ContactType.ConnectedPhone, contactInfo);
							DbSession.Save(contact);
						}
					}
					var message = new MailMessage();
					message.To.Add(mailToAdress);
					message.Subject = "Принято новое обращение клиента";
					message.From = new MailAddress("internet@ivrn.net");
					message.Body = Text.ToString();
					var smtp = new Mailer();
					smtp.SendText(message);
					RedirectToAction("MessageSended", new Dictionary<string, string> { { "clientName", clientName } });
				}
			}

			DbSession.Evict(client);
		}

		[return: JSONReturnBinder]
		public object Assist(string term)
		{
			if (string.IsNullOrEmpty(term))
				return new object[0];

			if (Request == null || Request.Headers["X-Requested-With"] != "XMLHttpRequest")
				return new object[0];

			var subs = term.Split(' ');

			return subs.SelectMany(s => DbSession.Query<Street>().Where(x => x.Name.Contains(s)))
				.Distinct()
				.Select(s => s.Name)
				.ToList();
		}

		[AccessibleThrough(Verb.Post)]
		public void Send([DataBind("application")] Request request)
		{
			if (Validator.IsValid(request)) {
				request.RegDate = DateTime.Now;
				request.ActionDate = DateTime.Now;
				if (AccessFilter.Authorized(Context)) {
					var clientId = Convert.ToUInt32(Session["LoginClient"]);
					var client = Client.Find(clientId);
					request.FriendThisClient = client;
				}
				var phoneNumber = request.ApplicantPhoneNumber.Substring(2, request.ApplicantPhoneNumber.Length - 2).Replace("-", string.Empty);
				request.ApplicantPhoneNumber = phoneNumber;
				request.Save();
				Flash["application"] = request;
				RedirectToAction("Ok");
			}
			else {
				var all = Tariff.FindAll();
				PropertyBag["tariffs"] = all;
				PropertyBag["application"] = request;
				RenderView("Zayavka");
			}
		}

		public void Warning()
		{
			var ipAddress = GetHost();
			if (!Client.Our(ipAddress, DbSession)) {
				RedirectToSiteRoot();
				return;
			}

			var origin = "";
			if (!string.IsNullOrEmpty(Request["host"])) {
				origin = Request["host"] + Request["url"];
			}
			PropertyBag["referer"] = origin;

			var lease = FindLease();

			ClientEndpoint endpoint;
			if (lease != null)
				endpoint = lease.Endpoint;
			else {
				var ips = DbSession.Query<StaticIp>().ToList();
				endpoint = ips.Where(ip => {
					if (ip.Ip == ipAddress.ToString())
						return true;
					if (ip.Mask != null) {
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						if (ipAddress.IsInSameSubnet(IPAddress.Parse(ip.Ip), subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault();
			}

			if (endpoint == null) {
				this.RedirectRoot();
				return;
			}

			var client = endpoint.Client;

			if (IsPost) {
				if (client.Disabled)
					RedirectToSiteRoot();

				if (client.ShowBalanceWarningPage) {
					client.ShowBalanceWarningPage = false;
					client.CreareAppeal("Отключена страница Warning, клиент отключил со страницы", AppealType.Statistic);
				}

				SceHelper.UpdatePackageId(DbSession, endpoint.Client);
				DbSession.Save(client);
				GoToReferer();
				return;
			}

			PropertyBag["Client"] = client;
		}

		private void SetEdatableAttribute(bool edit, string viewName)
		{
			PropertyBag["Content"] = DbSession.Query<SiteContent>().First(c => c.ViewName == viewName).Content;
			if (edit) {
				LayoutName = "TinyMCE";
				PropertyBag["ShowEditLink"] = false;
			}
			else {
				PropertyBag["ShowEditLink"] = true;
			}
		}
		public void GoToReferer(string name = "referer")
		{
			var url = Request.Form[name];
			if (String.IsNullOrEmpty(url))
				this.RedirectRoot();
			else
				RedirectToUrl(string.Format("http://{0}", url));
		}
	}
}