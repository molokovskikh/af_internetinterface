using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SmsController : BaseController
	{
		public void SmsIndex(uint clientId)
		{
			PropertyBag["messages"] = DbSession.Query<SmsMessage>().Where(s => s.Client != null && s.Client.Id == clientId).OrderByDescending(s => s.CreateDate).ToList();
			PropertyBag["client"] = DbSession.Load<Client>(clientId);
		}

		[AccessibleThrough(Verb.Post)]
		public void SendSms(string messageText, uint clientId, uint phoneId)
		{
			if (!string.IsNullOrEmpty(messageText)) {
				var phoneNumber = DbSession.Load<Contact>(phoneId);
				var message = new SmsMessage() {
					Client = DbSession.Load<Client>(clientId),
					CreateDate = DateTime.Now,
					Registrator = InitializeContent.Partner,
					PhoneNumber = "+7" + phoneNumber.Text,
					Text = messageText
				};

				new SmsHelper().SendMessage(message);

				Notify("Сообщение передано для отправки");
			}
			else {
				Error("Введите текст сообщения");
			}
			RedirectToReferrer();
		}

		[return: JSONReturnBinder]
		public string GetSmsStatus(uint smsId)
		{
			return new SmsHelper().GetStatus(DbSession.Get<SmsMessage>(smsId));
		}
	}
}