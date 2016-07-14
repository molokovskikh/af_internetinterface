using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class EmailNotification : Task
	{
		private static ILog log = LogManager.GetLogger(typeof(Program));

		public EmailNotification()
		{
		}

		public EmailNotification(ISession session) : base(session)
		{
		}

		protected override void Process()
		{
			SendMessages();
		}

		public bool Compare(decimal valDecimal, int valInt)
		{
			Decimal valDecimalB = Convert.ToDecimal(valInt);
			return valDecimalB < valDecimal;
		}

		public void SendMessages()
		{
			var timeToSendMail = ConfigurationManager.AppSettings["SendPremoderatedPomotionListAt"]
				.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			var timeToSendMailHour = int.Parse(timeToSendMail[0]);
			var timeToSendMailMinutes = timeToSendMail.Length > 1 ? int.Parse(timeToSendMail[1]) : 0;
			var mailTime = SystemTime.Now().Date.AddHours(timeToSendMailHour).AddMinutes(timeToSendMailMinutes);

			if (ConfigurationManager.AppSettings["EmailNotificationEnabled"] != null && SystemTime.Now() >= mailTime && SystemTime.Now() < mailTime.AddMinutes(30)) {
				var smtp = new Mailer();
				var messages = Session.Query<Contact>()
					.Where(m => m.Type == ContactType.NotificationEmailConfirmed
					            && m.Client.Disabled == false && m.Client.Disabled == false
					            && m.Client.PhysicalClient != null && m.Client.RatedPeriodDate != null); // на данный момент это только для физ.лиц
				var errorList = new List<Contact>();
				foreach (var item in messages) {
					if (item.Client.PhysicalClient != null
					    && item.Client.PhysicalClient.Balance <= (item.Client.GetSumForRegularWriteOff()
					                                              * int.Parse(ConfigurationManager.AppSettings["EmailNotificationMinRegularWriteoffCount"]))) {
						if (!string.IsNullOrEmpty(item.Text)) {
							try {
								var message = new MailMessage();
								message.To.Add(item.Text);
								message.Subject = "Уведомление о низком балансе";
								message.From = new MailAddress(ConfigurationManager.AppSettings["EmailNotificationFrom"]);
								message.Body = $"Ваш баланс составляет {item.Client.PhysicalClient.Balance} руб. При непоступлении оплаты доступ в сеть будет заблокирован в течение суток.";
								smtp.SendText(message);
							}
							catch (Exception) {
								errorList.Add(item);
							}
						}
					}
				}
				if (errorList.Count > 0) {
					var message = new MailMessage();
					ConfigurationManager.AppSettings["EmailNotificationError"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Each(s => { message.To.Add(s); });
					message.Subject = "Internet.Background не смог отправить уведомления";
					message.IsBodyHtml = true;
					message.From = new MailAddress("service@analit.net");
					message.Body = $"Не удалось отправить уведомления о низком балансе следующим адресатам:<br/>  {string.Join(",<br/>", errorList.Select(s => $"<a href='http://stat.ivrn.net/cp/Client/{(s.Client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal")}/{s.Client.Id}' >{s.Client.Id}</a> - " + $"<a href='mailto:{s.Text}?Subject=Уведомление%20о%20низком%20балансе' target='_top'>{s.Text}</a>  ").ToList())}";
					smtp.SendText(message);
				}
			}
		}
	}
}