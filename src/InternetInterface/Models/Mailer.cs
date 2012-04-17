﻿using System;
using System.Linq;
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
		{}

		public Mailer()
		{}

		public Mailer Invoice(Invoice invoice)
		{
			Layout = "Print";
			Template = "Invoice";
			IsBodyHtml = true;

			From = "internet@ivrn.net";
			To = invoice.Client.Contacts.Where(c => c.Type == ContactType.Email).Implode(c => c.Text);
			Subject = String.Format("Счет за {0}", invoice.Period);

			PropertyBag["invoice"] = invoice;

			return this;
		}
	}
}