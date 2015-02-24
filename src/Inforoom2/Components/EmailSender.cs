using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using Inforoom2.Models;

namespace Inforoom2.Components
{
	public class EmailSender
	{
		public static void SendEmail(PhysicalClient user, string subject, string body)
		{
			SendEmail(user.Email, subject, body);
		}

		public static void SendEmail(IList<PhysicalClient> users, string subject, string body)
		{
			foreach (var physicalClient in users) {
				SendEmail(physicalClient.Email, subject, body);
			}
		}

		public static void SendEmail(string[] to, string subject, string body)
		{
			foreach (var s in to) {
				SendEmail(s, subject, body);
			}
		}

		public static void SendEmail(string to, string subject, string body)
		{
			var mail = new MailMessage();
			mail.To.Add(to);
			mail.From = new MailAddress(ConfigurationManager.AppSettings["MailSenderAddress"]);
			mail.Subject = subject;
			mail.Body = body;
			mail.IsBodyHtml = true;
			SmtpClient smtp = new SmtpClient();
			smtp.Host = ConfigurationManager.AppSettings["SmtpServer"];
			smtp.Port = 25;
			smtp.UseDefaultCredentials = false;
			smtp.Send(mail);
		}

		public static void SendError(string message)
		{
			var service = ConfigurationManager.AppSettings["ErrorEmail"];
			if (string.IsNullOrEmpty(service)) {
				return;
			}
			message = "<pre>" + message + "</pre>";
			var mail = new MailMessage();
			mail.To.Add(service);
			mail.From = new MailAddress(service);
			mail.Subject = "Ошибка в Inforoom2";
			mail.Body = message;
			mail.IsBodyHtml = true;
			SmtpClient smtp = new SmtpClient();
			smtp.Host = ConfigurationManager.AppSettings["SmtpServer"];
			smtp.Port = 25;
			smtp.UseDefaultCredentials = false;
			smtp.Send(mail);
		}

		public static void SendDebugInfo(string title, string body)
		{
			var email = ConfigurationManager.AppSettings["DebugInfoEmail"];
			if (string.IsNullOrEmpty(email)) {
				return;
			}
			SendEmail(email,title,body);
		}
	}
}