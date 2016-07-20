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
using System.Configuration;
using System.Net.Mail;

namespace InternetInterface.Background
{
	public class ConnectedHousesUpdate : Task
	{
		public ConnectedHousesUpdate()
		{
		}

		public ConnectedHousesUpdate(ISession session) : base(session)
		{
		}

		protected override void Process()
		{
			UpdateConnections();
		}

		public void UpdateConnections()
		{
			var timeToSendMail = ConfigurationManager.AppSettings["ConnectedHousesUpdateAt"]
				.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			var timeToSendMailHour = int.Parse(timeToSendMail[0]);
			var timeToSendMailMinutes = timeToSendMail.Length > 1 ? int.Parse(timeToSendMail[1]) : 0;
			var mailTime = SystemTime.Now().Date.AddHours(timeToSendMailHour).AddMinutes(timeToSendMailMinutes);

			if (SystemTime.Now() >= mailTime && SystemTime.Now() < mailTime.AddMinutes(30)) {
				try {
					House.SynchronizeHouseConnections(Session);
				}
				catch (Exception ex) {
					var smtp = new Mailer();
					var message = new MailMessage();
					ConfigurationManager.AppSettings["EmailNotificationError"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Each(s => { message.To.Add(s); });
					message.Subject = "Internet.Background не смог обновить список подключенных домов";
					message.IsBodyHtml = true;
					message.From = new MailAddress("service@analit.net");
					message.Body = $"Не удалось обновить список подключенных домов:<br/>---<br/>" + ex.Message + "<br/>---<br/>" + ex.StackTrace;
					smtp.SendText(message);
				}
			}
		}
	}
}