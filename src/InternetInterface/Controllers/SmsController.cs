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

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SmsController : BaseController
	{
		public void SmsIndex(uint clientId)
		{
			PropertyBag["messages"] = SmsMessage.Queryable.Where(s => s.Client != null && s.Client.Id == clientId).OrderByDescending(s => s.CreateDate).ToList();
			PropertyBag["client"] = Client.Find(clientId);
		}

		[AccessibleThrough(Verb.Post)]
		public void SendSms(string messageText, uint clientId, uint phoneId)
		{
			if (!string.IsNullOrEmpty(messageText))
			{
				var phoneNumber = Contact.Find(phoneId);
				var message = new SmsMessage
				{
					Client = Client.Find(clientId),
					CreateDate = DateTime.Now,
					Registrator = InitializeContent.Partner,
					PhoneNumber = "+7" + phoneNumber.Text,
					Text = messageText
				};
#if !DEBUG
				SmsHelper.SendMessage(message);
#endif
				Flash["Message"] = Message.Notify("Сообщение передано для отправки");
			} else {
				Flash["Message"] = Message.Error("Введите текст сообщения");
			}
			RedirectToReferrer();
		}
	}
}