using System;
using System.IO;
using System.Linq;
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

		public Mailer Invoice(Invoice invoice)
		{
			Layout = "Print";
			Template = "Invoice";
			IsBodyHtml = true;

			From = "internet@ivrn.net";
			To = invoice.Client.Contacts.Where(c => c.Type == ContactType.Email).Implode(c => c.Text);
#if DEBUG
			To = "kvasovtest@analit.net";
#endif
			Subject = String.Format("Счет за {0}", invoice.Period);

			PropertyBag["invoice"] = invoice;

			return this;
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