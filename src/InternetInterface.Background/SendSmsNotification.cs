using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class SendSmsNotification : Task
	{
		public SendSmsNotification()
		{
		}

		public SendSmsNotification(ISession session) : base(session)
		{
		}

		protected override void Process()
		{
			SendMessages();
		}

		public List<SmsMessage> SendMessages()
		{
			var messages = Session.Query<SmsMessage>().Where(m =>
				!m.IsSended
					&& m.PhoneNumber != null
					&& m.ShouldBeSend == null || (m.ShouldBeSend > DateTime.Today && m.ShouldBeSend < DateTime.Today.AddDays(1)))
				.ToList();
			if (messages.Count > 0)
				new SmsHelper().SendMessages(messages);
			return messages;
		}
	}
}
