﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Background
{
	public class MailEndpointProcessor
	{
		public void Process()
		{
			var smtp = new SmtpClient("box.analit.net");
#if !DEBUG
			var mailToAdress = "internet@ivrn.net";
#else
			var mailToAdress = "a.zolotarev@analit.net";
#endif
			using (new SessionScope()) {
				var guestLeases = Lease.Queryable.Where(l => l.Endpoint == null).ToList();
				var sended_leases =
					ArHelper.WithSession(
						s => {
							if (guestLeases.Count <= 0)
								return new List<uint>();
							var query = string.Format(@"
select sl.LeaseId from internet.SendedLeases sl
where sl.LeaseId in ({0})", guestLeases.Select(g => g.Id).Implode());
							return s.CreateSQLQuery(query).List<uint>().ToList();
						});
				var text = new StringBuilder();
				foreach (var gl in guestLeases.Where(g => !sended_leases.Contains(g.Id))) {
					new SendedLease {
						LeaseId = gl.Id,
						Ip = gl.Ip,
						LeaseBegin = gl.LeaseBegin,
						LeaseEnd = gl.LeaseEnd,
						LeasedTo = gl.LeasedTo,
						Module = gl.Module,
						Port = gl.Port,
						Switch = gl.Switch,
						Pool = gl.Pool,
						SendDate = DateTime.Now
					}.Save();
					text.AppendLine("Клиент:");
					if (gl.Switch != null)
						text.AppendLine(string.Format("Свитч: {0} ({1})", gl.Switch.Name, gl.Switch.GetNormalIp()));
					else
						text.AppendLine("Свитч не определен");
					text.AppendLine("Порт: " + gl.Port);
					text.AppendLine("MAC: " + gl.LeasedTo);
					text.AppendLine("---------------------------------");
				}
				if (!string.IsNullOrEmpty(text.ToString())) {
					var message = new MailMessage();
					message.To.Add(mailToAdress);
					message.Subject = "Неизвесный клиент";
					message.From = new MailAddress("service@analit.net");
					message.Body = text.ToString();
					smtp.Send(message);
				}
			}
		}
	}
}