using System.Configuration;
using System.Net.Mail;
using Inforoom2.Models;

namespace Inforoom2.Components
{
	public class EmailSender 
	{
		public static void SendEmail(Client user, string subject, string body)
		{
			var mail = new MailMessage();
#if !DEBUG
			mail.To.Add(user.Email);
#else
			mail.To.Add("efedorov@analit.net");
#endif
			mail.From = new MailAddress(ConfigurationManager.AppSettings["MaileSenderAddress"]);
			mail.Subject = subject;
			mail.Body = body;
			mail.IsBodyHtml = true;
			SmtpClient smtp = new SmtpClient();
			smtp.Host = ConfigurationManager.AppSettings["SmtpServer"];
			smtp.Port = 25;
			smtp.UseDefaultCredentials = false;
			smtp.Send(mail);
		}
	}
}