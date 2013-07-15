using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class SendSmsNotification : Task
	{
		protected override void Process()
		{
			var thisDateMax = Session.Query<InternetSettings>().First();
			var now = SystemTime.Now();
			if ((thisDateMax.NextSmsSendDate - now).TotalMinutes <= 0) {
				SendSms();
				if (now.Hour < 12) {
					thisDateMax.NextSmsSendDate = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);
					Session.Save(thisDateMax);
				}
			}
		}

		public IList<SmsMessage> SendSms()
		{
			var messages = Session.Query<SmsMessage>().Where(m => !m.IsSended && m.PhoneNumber != null).ToList();
			new SmsHelper().SendMessages(messages);
			var thisDateMax = Session.Query<InternetSettings>().First();
			thisDateMax.NextSmsSendDate = SystemTime.Now().AddDays(1).Date.AddHours(12);
			Session.Update(thisDateMax);
			return messages;
		}
	}
}
