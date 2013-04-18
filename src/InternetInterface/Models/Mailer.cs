using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Castle.Core.Smtp;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models
{
	public static class MailerExtension
	{
		public static Mailer Mailer(this SmartDispatcherController controller)
		{
			return controller.Mailer<Mailer>();
		}
	}

	public class Mailer : BaseMailer
	{
		public Mailer(IEmailSender sender) : base(sender)
		{
		}

		public Mailer()
		{
		}

		public void SendText(string from, string to, string subject, string body)
		{
			var mailMessage = new MailMessage(from, to, subject, body);
#if DEBUG
			mailMessage = new MailMessage(from, "kvasovtest@analit.net", subject, body);
#endif
			Sender.Send(mailMessage);
		}

		public Mailer UserWriteOff(UserWriteOff writeOff)
		{
			Template = "UserWriteOff";
			From = "internet@ivrn.net";
			To = "InternetBilling@analit.net";
			Subject = "Списание для Юр.Лица.";
#if DEBUG
			To = "kvasovtest@analit.net";
#endif
			var registrator = writeOff.Registrator != null ? writeOff.Registrator.Name : string.Empty;
			PropertyBag["registrator"] = registrator;
			PropertyBag["writeOff"] = writeOff;

			return this;
		}
	}
}