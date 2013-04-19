using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using InternetInterface.Models;
using log4net;

namespace Billing
{
	public class EmailNotificationSender
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(EmailNotificationSender));

		public static bool Send(StringBuilder messageText, string subject)
		{
			try {
				var mailToAdress = "internet@ivrn.net";
#if DEBUG
				mailToAdress = "kvasovtest@analit.net";
#endif
				var message = new MailMessage();
				message.To.Add(mailToAdress);
				message.Subject = subject;
				message.From = new MailAddress("service@analit.net");
				message.Body = messageText.ToString();
				var smtp = new SmtpClient("box.analit.net");
				smtp.Send(message);
				return true;
			}
			catch (Exception ex) {
				_log.Error("Ошибка отправки письма EmailNotificationSender", ex);
				return false;
			}
		}

		public static bool SendLawyerPersonNotification(Client client)
		{
			var messageText = new StringBuilder();
			messageText.AppendLine(string.Format("Клиент {0} имеет задолженность {1} руб.", client.Id.ToString("00000"), client.Balance));
			messageText.AppendLine(string.Format("Название - \"{0}\"", client.Name));
			messageText.AppendLine(string.Format("Тариф - \"{0}\"", client.LawyerPerson.Tariff));
			return Send(messageText, "Возникла задолженность клиента");
		}

		public static bool SendCloseServiceAndNoWriteOff(OrderService service, Client client)
		{
			var messageText = new StringBuilder();
			messageText.AppendLine(string.Format("У клиента {0} был закрыл заказ №{1}.", client.Id.ToString("00000"), service.Order.Id));
			messageText.AppendLine(string.Format("Списание по услуге - \"{0}\" ({1})", service.Description, service.Id));
			messageText.AppendLine("За текущий месяц не найдено, корректировка не произведена");
			return Send(messageText, "Не произведена корректировка списаний клиента");
		}
	}
}