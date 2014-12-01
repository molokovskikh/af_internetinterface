using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class SendNullTariffLawyerPerson : Task
	{
		public SendNullTariffLawyerPerson()
		{
		}

		public SendNullTariffLawyerPerson(ISession session) : base(session)
		{
		}

		protected override void Process()
		{
			var nullLeases = Session.Query<Lease>().Where(l =>
				l.Pool.IsGray &&
					(l.Endpoint.Client.LawyerPerson != null &&
						l.Endpoint.Client.Disabled))
				.ToList();
			var sndingLease = Session.Query<SendedLease>().Where(s => s.SendDate >= DateTime.Now.Date).Select(s => s.LeaseId).ToList();
			var smtp = new Mailer();
#if !DEBUG
			var mailToAdress = "ibilling@ivrn.net";
#else
			var mailToAdress = "kvasovtest@analit.net";
#endif
			var text = new StringBuilder();
			foreach (var lease in nullLeases.Where(l => !sndingLease.Contains(l.Id))) {
				Session.Save(new SendedLease(lease));
				text.AppendLine("Обнаружена активность отключенного юр. лица");
				text.AppendLine(string.Format("\"{0}\" пытается выйти в интернет, но из-за незаданной абонентской платы, получает серый Ip {1} .", lease.Endpoint.Client.Name, lease.Ip));
				text.AppendLine("Ответственным лицам необходимо обратить внимание: счет №" + lease.Endpoint.Client.Id);
				var message = new MailMessage();
				message.To.Add(mailToAdress);
				message.Subject = "Подозрительный клиент";
				message.From = new MailAddress("service@analit.net");
				message.Body = text.ToString();
				smtp.SendText(message);
			}
		}
	}
}
