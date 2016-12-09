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
using Common.Tools.Calendar;

namespace InternetInterface.Background
{
	public class LegalClientDebtMail : Task
	{
		public LegalClientDebtMail()
		{
		}

		public LegalClientDebtMail(ISession session) : base(session)
		{
		}

		protected override void Process()
		{
			TryToSendLegalClientDebtMails();
		}

		private enum Months
		{
			Январь = 1,
			Февраль = 2,
			Март = 3,
			Апрель = 4,
			Май = 5,
			Июнь = 6,
			Июль = 7,
			Август = 8,
			Сентябрь = 9,
			Октябрь = 10,
			Ноябрь = 11,
			Декабрь = 12
		}

		public void TryToSendLegalClientDebtMails()
		{
			const string messageTextFormat = @"
Добрый день!
Обращаем Ваше внимание, что {0} в 22:00 произойдет списание за {1} ({2} руб.) и Ваша задолженность составит {3} руб.,
доступ в сеть с {4} будет заблокирован. Во избежание блокировки необходимо оплатить задолженность в полном объеме.
Благодарим за сотрудничество.
По всем вопросам обращайтесь по телефону {5}.
С уважением, Ваш 'Инфорум'";

			var daysBeforeWriteOff = Convert.ToInt32(ConfigurationManager.AppSettings["SendLegalClientDebtMailForDays"]);
			if ((SystemTime.Today().LastDayOfMonth().AddDays(1) - SystemTime.Now()).Days > daysBeforeWriteOff) {
				return;
			}
			var timeToSendMail = ConfigurationManager.AppSettings["SendLegalClientDebtMailAt"]
				.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);

			var timeToSendMailHour = int.Parse(timeToSendMail[0]);
			var timeToSendMailMinutes = timeToSendMail.Length > 1 ? int.Parse(timeToSendMail[1]) : 0;
			var mailTime = SystemTime.Now().Date.AddHours(timeToSendMailHour).AddMinutes(timeToSendMailMinutes);
			
			if (SystemTime.Now() >= mailTime && SystemTime.Now() < mailTime.AddMinutes(30)) {
				var smtp = new Mailer();
				var messages = Session.Query<Contact>().Where(s => s.Type == ContactType.Email
					&& s.Client.LawyerPerson != null && s.Client.Status.Id == (int) StatusType.Worked);

				var errorList = new List<Contact>();
				foreach (var item in messages) {
					if (item.Client.LawyerPerson.Balance < 0 && item.Client.LawyerPerson.Tariff.HasValue
						&& Math.Abs(item.Client.LawyerPerson.Balance) >= item.Client.LawyerPerson.Tariff.Value) {
						if (!string.IsNullOrEmpty(item.Text)) {
							try {
								var message = new MailMessage();
								message.To.Add(item.Text);
								message.Subject = "Уведомление о низком балансе";
								message.From = new MailAddress(ConfigurationManager.AppSettings["EmailNotificationFrom"]);
								message.IsBodyHtml = false;
								message.Body = string.Format(messageTextFormat, SystemTime.Today().LastDayOfMonth().ToShortDateString(),
									((Months) SystemTime.Now().Month).ToString(), item.Client.LawyerPerson.Tariff.Value.ToString("F2"),
									Math.Abs(item.Client.LawyerPerson.Balance - item.Client.LawyerPerson.Tariff.Value).ToString("F2"),
									SystemTime.Today().LastDayOfMonth().AddDays(1).ToShortDateString(),
									item.Client.LawyerPerson.Region.RegionOfficePhoneNumber);
								smtp.SendText(message);
							} catch (Exception) {
								errorList.Add(item);
							}
						}
					}
				}
				if (errorList.Count > 0) {
					var message = new MailMessage();
					ConfigurationManager.AppSettings["EmailNotificationError"].Split(new char[] {','},
						StringSplitOptions.RemoveEmptyEntries).Each(s => { message.To.Add(s); });
					message.Subject = "Internet.Background не смог отправить уведомления о задолжности юр.лиц.";
					message.IsBodyHtml = true;
					message.From = new MailAddress("service@analit.net");
					message.Body =
						$"Не удалось отправить уведомления о низком балансе следующим адресатам:<br/>  {string.Join(",<br/>", errorList.Select(s => $"<a href='http://stat.ivrn.net/cp/Client/{(s.Client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal")}/{s.Client.Id}' >{s.Client.Id}</a> - " + $"<a href='mailto:{s.Text}?Subject=Уведомление%20о%20низком%20балансе' target='_top'>{s.Text}</a>  ").ToList())}";
					smtp.SendText(message);
				}
			}
		}
	}
}