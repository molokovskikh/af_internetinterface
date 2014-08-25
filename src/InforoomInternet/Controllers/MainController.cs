﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Castle.ActiveRecord;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Editor;
using InforoomInternet.Helpers;
using InforoomInternet.Models;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class MainController : BaseController
	{
		public void Index()
		{
			PropertyBag["tariffs"] = DbSession.Query<Tariff>().Where(t => !t.Hidden).ToList();
		}

		public void Zayavka()
		{
			var request = new Request();
			if (AccessFilter.Authorized(Context)) {
				var client = DbSession.Load<Client>(Convert.ToUInt32(Session["LoginClient"]));
				request.FriendThisClient = client;
			}

			PropertyBag["tariffs"] = DbSession.Query<Tariff>().Where(t => !t.Hidden).ToList();
			PropertyBag["request"] = request;
			if (IsPost) {
				Bind(request, "request");
				if (IsValid(request)) {
					request.PreInsert();
					DbSession.Save(request);
					RedirectToAction("Ok", new { id = request.Id });
				}
			}
		}

		public void Main()
		{
		}

		public void HowPay(bool edit)
		{
			SetEdatableAttribute(edit, "HowPay");
		}

		public void Ok(uint id)
		{
			PropertyBag["application"] = DbSession.Load<Request>(id);
		}

		public void OfferContract(bool edit)
		{
		}

		public void PrivateOffice()
		{
		}

		public void Feedback(string clientName, string contactInfo, uint clientId, string appealText, uint autoClientId)
		{
			var mailToAdress = "internet@ivrn.net";
#if DEBUG
			mailToAdress = "kvasovtest@analit.net";
#endif
			var clientEndPoint = IpAdressHelper.GetClientEndpoint(Request.UserHostAddress, DbSession);
			var client = clientEndPoint != null ? clientEndPoint.Client : null;
			client = client ?? new Client();
			PropertyBag["client"] = client;

			if (IsPost) {
				var Text = new StringBuilder();
				Text.AppendLine("Обращение клиента через сайт WWW.IVRN.NET \r\n");
				Text.AppendLine("Клиент пришел с адреса: " + Request.UserHostAddress);
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

		public void Warning()
		{
			var origin = "";
			if (!string.IsNullOrEmpty(Request["host"])) {
				origin = Request["host"] + Request["url"];
			}
			PropertyBag["referer"] = origin;
			var endpoint = IpAdressHelper.GetClientEndpoint(Request.UserHostAddress, DbSession);
			if (endpoint == null || endpoint.Client == null) {
				RedirectToSiteRoot();
				return;
			}

			var client = endpoint.Client;
			if (IsPost) {
				if (client.Status.Type == StatusType.BlockedForRepair) {
					client.SetStatus(StatusType.Worked, DbSession);
				}
				else if (client.Disabled) {
					RedirectToSiteRoot();
				}
				else if (client.ShowBalanceWarningPage) {
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
				RedirectToSiteRoot();
			else
				RedirectToUrl(string.Format("http://{0}", url));
		}
	}
}