using System.Net.Mail;
using Castle.Core.Smtp;
using Common.Web.Ui.MonoRailExtentions;

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
		}

		private void Init()
		{
			From = "internet@ivrn.net";
			To = "internet@ivrn.net";
		}

		public void SendText(string from, string to, string subject, string body)
		{
			To = to;
			From = from;
			Subject = subject;

			var mailMessage = new MailMessage(from, to, subject, body);
#if DEBUG
			mailMessage = new MailMessage(from, "kvasovtest@analit.net", subject, body);
#endif
			Sender.Send(mailMessage);
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
	}
}