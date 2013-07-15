using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Background
{
	public class SendUnknowEndPoint : Task
	{
		protected override void Process()
		{
			var smtp = new SmtpClient();
#if !DEBUG
			var mailToAdress = "internet@ivrn.net";
#else
			var mailToAdress = "kvasovtest@analit.net";
#endif
			var guestLeases = Session.Query<Lease>().Where(l => l.Endpoint == null).ToList();

			var sended_leases = new List<uint>();
			if (guestLeases.Count > 0) {
				var query = string.Format(@"
select sl.LeaseId from internet.SendedLeases sl
where sl.LeaseId in ({0})", guestLeases.Select(g => g.Id).Implode());
				sended_leases = Session.CreateSQLQuery(query).List<uint>().ToList();
			}


			var text = new StringBuilder();
			foreach (var gl in guestLeases.Where(g => !sended_leases.Contains(g.Id))) {
				Session.Save(new SendedLease(gl));
				text.AppendLine("Клиент:");
				if (gl.Switch != null) {
					text.AppendLine(string.Format("Коммутатор: {0} ({1})", gl.Switch.Name, gl.Switch.IP));
					text.AppendLine("Зона: " + gl.Switch.Zone);
				}
				else
					text.AppendLine("Коммутатор не определен");
				text.AppendLine("Порт: " + gl.Port);
				text.AppendLine("MAC: " + gl.LeasedTo);
				text.AppendLine("---------------------------------");
			}
			if (!string.IsNullOrEmpty(text.ToString())) {
				var message = new MailMessage();
				message.To.Add(mailToAdress);
				message.Subject = "Неизвестный клиент";
				message.From = new MailAddress("service@analit.net");
				message.Body = text.ToString();
				smtp.Send(message);
			}
		}
	}
}
