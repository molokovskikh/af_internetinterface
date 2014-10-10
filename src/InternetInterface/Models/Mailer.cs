using System;
using System.Configuration;
using System.Management.Instrumentation;
using System.Net.Mail;
using Castle.Core.Smtp;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using NHibernate.Mapping;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{
	public class Mailer : BaseMailer
	{
		public Mailer(IEmailSender sender) : base(sender)
		{
			Init();
		}

		public Mailer()
		{
			Init();
			Sender = new FolderSender(ConfigurationManager.AppSettings["SmtpServer"]);
		}

		private void Init()
		{
			From = "internet@ivrn.net";
			To = "internet@ivrn.net";
		}

		public void SendText(string from, string to, string subject, string message)
		{
			String[] arr = {to};
			SendText(from, arr, subject, message);
		}

		public void SendText(string from, string[] toEmails, string subject, string message)
		{
			From = from;
			Subject = subject;
#if DEBUG
			foreach (var email in toEmails) 
				message = email + "\n" + message;
			var mail = ConfigurationManager.AppSettings["DebugMail"];
			if(mail == null)
				throw new Exception("Параметр приложения DebugMail должен быть задан в config");
			toEmails = new string[1] {mail};
#endif
			foreach (var email in toEmails) {
				var mailMessage = new MailMessage(from, email, subject, message);
				Sender.Send(mailMessage);
			}
		}

		public void SendText(MailMessage message)
		{
			Sender.Send(message);
		}

		public Mailer SmsSendUnavailable(ServiceRequest request)
		{
			Template = "SmsSendUnavailable";
			Subject = "Отправка смс";
			PropertyBag["request"] = request;

			return this;
		}

		public Mailer UserWriteOff(UserWriteOff writeOff)
		{
			Template = "UserWriteOff";
			To = "InternetBilling@analit.net";
			Subject = "Списание для Юр.Лица.";
			var registrator = writeOff.Registrator != null ? writeOff.Registrator.Name : string.Empty;
			PropertyBag["registrator"] = registrator;
			PropertyBag["writeOff"] = writeOff;

			return this;
		}

		public Mailer PaymentMoved(Payment payment, Client source, PaymentMoveAction action)
		{
			Template = "PaymentMoved";
			To = "InternetBilling@analit.net";
			Subject = "Перемещен платеж";
			PropertyBag["payment"] = payment;
			PropertyBag["source"] = source;
			PropertyBag["action"] = action;
			PropertyBag["currentUser"] = InitializeContent.Partner;
			return this;
		}
	}
}